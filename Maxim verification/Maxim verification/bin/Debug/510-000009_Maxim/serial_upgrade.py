#!/usr/bin/env python3

from __future__ import print_function

import sys
import csv
import os
import traceback
import argparse
import struct
import serial

is_py2 = sys.version[0] == '2'
if is_py2:
    import Queue as queue
else:
    import queue as queue

import threading
import time
import math


try:
    from serial.tools.list_ports import comports
except ImportError:
    print("Can't import the port finder. You will have to manually specify the port")
    comports = None

from datetime import datetime  as dt
from datetime import timedelta

version_major = '3'
version_minor = '3'
version_point = '0'
version_extn  = 'dbg13'

debug = False

# Packet type indicators
ACK = 'ACK'
NAK = 'NAK'
TIMEOUT = 'TIMEOUT'
CONSOLE = 'CONSOLE'
CHKFAIL = 'CHKFAIL'
PROGFAIL = 'PROGFAIL'
CRCFAIL  = 'CRCFAIL'
TOOSMALL = 'TOOSMALL'
UNSTUFF  = 'UNSTUFF'
UNHANDLED_WUFF = 'UNHANDLED_WUFF'
CONSOLE_TYPE = 'CONSOLE'
MFG_RESPONSE_TYPE = 'MFG_RESP'

STATE_ENABLED       = 'enabled'
STATE_DISABLED      = 'disabled'
NFC_FIELD_ON        = 'on'
NFC_FIELD_PULSE     = 'pulse'

WUFF_Q02            = 'Q02'
WUFF_Q_CMD_RESP     = 'Q06'
WUFF_Q_CONSOLE      = 'Q08'
WUFF_Q_CONSOLE_BUG  = 'QQ08'
WUFF_Q_FW           = 'Q11'
WUFF_Q_FW_RESP      = 'Q12'
WUFF_Q_ACK          = 'ack'
WUFF_LF             = b'\x0a'
WUFF_Q_OUT_SYNC     = 'Q01,sync\n'
WUFF_Q_OUT_EMPTY    = '\n'
WUFF_Q_FW_PREFIX    = WUFF_Q_FW + ','
WUFF_Q_PROMPT       = 'Q08,>'

WUFF_W02            = 'W02'
WUFF_W_OUT_SYNC     = 'W01,sync\n'
WUFF_W_ACK          = 'ack'

WUFF_X02            = 'X02'
WUFF_X_CMD_RESP     = 'X06'
WUFF_X_OUT_SYNC     = 'X01,sync\n'
WUFF_X_ACK          = 'ack'
WUFF_X_NFC_MODE4    = 'X05,01,24,4,68A3\n'
WUFF_X_NFC_MODE3    = 'X05,02,24,3,5892\n'

WUFF_CMDRESP_SUCCESS        = 1
WUFF_CMDRESP_FAILURE        = 2

WUFF_FWRESP_SUCCESS         = 1
WUFF_FWRESP_TOOSMALL        = 2
WUFF_FWRESP_UNSTUFF_FAIL    = 3
WUFF_FWRESP_CRCFAIL         = 4
WUFF_FWRESP_PROGFAIL        = 5

WUFF_CMD_REBOOT             = 2
WUFF_CMD_START_FW           = 3
WUFF_CMD_END_FW             = 4

WUFF_BPKCMD_SET_FMODE       = 24

WUFF_CMDRESP_SEQUENCE_LOC   = 0
WUFF_CMDRESP_CMD_LOC        = 1
WUFF_CMDRESP_RESULT_LOC     = 2
WUFF_MFGRESP_CMD_LOC        = 0
WUFF_MFGRESP_RESULT_LOC     = 2
WUFF_FWRESP_RESULT_LOC      = 0

MFG_CMD_RESPONSE            = ':m'
MFG_CMD_PASS                = 'pass'
MFG_CMD_OK                  = 'OK'
MFG_CMD_ENABLE_QUIET_MODE       = 'm085 1\n'
MFG_CMD_DISABLE_QUIET_MODE      = 'm085 2\n'
MFG_CMD_OLD_CHARGING_DISABLE    = 'm087\n'
MFG_CMD_CHARGING_DISABLE        = 'm091\n'
MFG_CMD_CHARGING_ENABLE         = 'm088\n'

# Default number of times a packet is tried in row before
# the trasfer aborts.
DEFAULT_SEND_RETRIES        = 100

def hdump(dat):
    lbreak = 1;
    for each in dat:
        print("%02x " % each ,end='')
        if not (lbreak % 20):
            print()
        lbreak+=1
    print()

class Firmware_serial_response:

    def __init__(self):
        self.pkt_type = 0
        self.build_state = 0
        self.rx_error = False
        self.empty()

    def empty(self):
        self.data = None
        self.pkt_valid = False
        self.raw_pkt = bytearray()
        self.build_state = 0
        self.error = False
        self.error_info = ''

    def has_error(self):
        return self.error

    def build(self,data):

        self.raw_pkt.extend(data)
        # All responses are LF terminated
        # Look for a LF
        if data == b'\x0a':
            return True

        return False

    def parse(self,packet=None):

        if packet is not None:
            self.raw_pkt = packet

        self.pkt_valid = False
        self.error = False

        # We have treated things as series of bytes
        # but since its all ASCII it will be easier to work
        # with as a string.

        try:
            response = self.raw_pkt.decode('ascii')
            # Update responses all start with
            # 'FIRM:' and then have and 'ACK:' or 'NAK:'.
            # Some additional info may follow the ACK/NAK field

            if response.startswith('FIRM:'):
                # Valid response. Parse it
                parsed = response.rstrip().split(':')
                self.pkt_valid = True
                self.data = parsed[1:]
            elif response.startswith('Q'):
                # Havard WUFF response
                if (response.startswith(WUFF_Q_CONSOLE)
                    or response.startswith(WUFF_Q_CONSOLE_BUG)
                    ):

                    # If it is a freeform console packet then only
                    # split on the 1st comma
                    parsed = response.rstrip().split(',', 1)
                else:
                    parsed = response.rstrip().split(',')
                self.pkt_valid = True
                self.data = parsed
            elif response.startswith('W'):
                #  NFC response
                parsed = response.rstrip().split(',')
                self.pkt_valid = True
                self.data = parsed
            elif response.startswith('X'):
                #  Bird response
                parsed = response.rstrip().split(',')
                self.pkt_valid = True
                self.data = parsed
            else:
                # Not a response packet.  May be a normaal
                # debug output string.
                # Mark data as not a valid response but
                # also not an error.
                self.pkt_valid = False
                self.error = False
                self.data = response

            return True

        except UnicodeError as err:
            # Some sort of decode error happened
            self.pkt_valid = False
            self.error = True
            self.error_info = err.reason + '\n'
            self.error_info += 'Value: 0x%02X @ location %d\n' % (err.object[err.start], err.start)
            self.error_info += 'PktHex: '
            for each in err.object:
                self.error_info += '%02X ' % each

        except:
            self.pkt_valid = False
            self.error = True
            self.error_info = f'Unkown Rx exception: {sys.exc_info()[0]}'

        return False

class SerialWriteThread(threading.Thread):
    def __init__(self, cmd_q=None, data_q=None, alive=None):
        super(SerialWriteThread, self).__init__()
        #self.cmd_q = cmd_q or Queue.Queue()
        self.data_q = data_q or queue.Queue()
        if alive is not None:
            self.alive = alive
        else:
            self.alive = threading.Event()
            self.alive.set()

        self.serial_connection = None

        # Delay in microseconds between each
        # byte in a frame
        # zero means no delay and the entire
        # frame is sent in a single write call

        self.byte_delay = 0

    def set_serial_connection(self,sercon):
        self.serial_connection = sercon

    def stop(self):
        self.alive.clear()

    def get_data_q(self):
        return self.data_q

    def run(self):
        print("Write thread up")

        do_byte_delay = False
        byte_delay_td = 0

        while self.alive.isSet():

            if self.byte_delay > 0:
                do_byte_delay = True
                byte_delay_td = timedelta(microseconds=self.byte_delay)
            else:
                do_byte_delay = False

            try:
                # Queue.get with timeout to allow checking self.alive
                data = self.data_q.get(True, 0.5)
                if len(data) > 0:

                    if do_byte_delay:
                        for each in data:
                            single_byte = bytearray()
                            single_byte.append(each)
                            self.serial_connection.write(single_byte)

                            begin = dt.now()

                            while (dt.now() - begin) < byte_delay_td:
                                pass
                    else:
                        self.serial_connection.write(serial.to_bytes(data))

                    data = None
            except queue.Empty as e:
                continue

        print("Write thread exit")

    def set_byte_delay(self, delay):
        self.byte_delay = delay

class SerialReadThread(threading.Thread):
    def __init__(self, cmd_q=None, data_q=None, alive=None):
        super(SerialReadThread, self).__init__()
        #self.cmd_q = cmd_q or Queue.Queue()
        self.data_q = data_q or queue.Queue()
        if alive is not None:
            self.alive = alive
        else:
            self.alive = threading.Event()
            self.alive.set()

        self.serial_connection = None
        self.rx_packet = Firmware_serial_response()

    def set_serial_connection(self,sercon):
        self.serial_connection = sercon

    def stop(self):
        self.alive.clear()

    def get_data_q(self):
        return self.data_q

    def run(self):
        print("Read thread up")

        self.rx_packet.empty()
        while self.alive.isSet():
            # Read bytes until we have a complete packet
            data = self.serial_connection.read(1)

            if len(data) == 0:
                # Read timeout
                continue

            if not self.rx_packet.build(data):
                # Not complete packet yet
                continue

            # Got a packet. Parse it and see if it's
            # A response
            self.rx_packet.parse()

            # If there are no errors then push the packet into the q
            if not self.rx_packet.has_error():
                self.data_q.put((self.rx_packet.pkt_valid,self.rx_packet.data))
            else:
                print()
                print(self.rx_packet.error_info)

            # Start over
            self.rx_packet.empty()

        print("Read thread exit")
        f = open("script_end.txt", "a")
        f.write("End Successfully!")
        f.close()


class WS_comm_threaded:
    """
        threaded cloass for reading data from a whoop strap vi
        rfcomm.
    """
    def __init__(self,device=None):
        self.device = device
        self.serial = None
        self.baud = None
        self.port = None
        self.use_twostops = False
        self.stopbits = None

        self.threads_run = threading.Event()

        self.write_thread = SerialWriteThread(alive=self.threads_run)
        self.outq = self.write_thread.get_data_q()

        self.read_thread = SerialReadThread(alive=self.threads_run)
        self.inq = self.read_thread.get_data_q()

    def open(self,device=None, baud=None):
        self.serial = None
        if device is not None:
            self.device = device

        if baud is not None:
            self.baud = baud

        if self.baud is None:
            self.baud = 115200

        if self.use_twostops:
            self.stopbits = 2
        else:
            self.stopbits = 1

        if self.device is None:
            self.device = "/dev/ttyUSB0"

        self.serial = serial.Serial(port=self.device, baudrate=self.baud, timeout=0.5, stopbits=self.stopbits)

        if self.serial is not None:
            return True
        else:
            return False

    def attach(self, instance):
        self.serial = instance

    def list_ports(self):
        if comports is not None:
            port_list = comports()
            if len(port_list) > 0:
                for each in port_list:
                    print(each.device)
            else:
                print('No ports found')

    def attach_threads(self):
        self.write_thread.set_serial_connection(self.serial)
        self.read_thread.set_serial_connection(self.serial)

    def get_read_queue(self):
        return self.read_thread.get_data_q()

    def get_write_queue(self):
        return self.write_thread.get_data_q()

    def start_threads(self):
        self.threads_run.set()
        self.write_thread.start()
        self.read_thread.start()

    def stop_threads(self):
        self.threads_run.clear()

    def get_timeout(self):
        if self.serial is None:
            return 0
        return self.serial.timeout

    def set_timeout(self,timeout=None):
        if timeout is not None and self.serial is not None:
            self.serial.timeout = timeout

    def send_break(self):
#        self.serial.send_break()
        self.serial.sendBreak()

    def q_puts(self,val):
        self.outq.put(val.encode('utf-8'))

    def set_byte_delay(self, delay):
        self.write_thread.set_byte_delay(delay)

    def set_use_twostops(self, twostops):
        self.use_twostops = twostops

    def get_use_twostops(self):
        return self.use_twostops

def wait_for_response(inq, timeout=0, show_console=True, q_expect=None, expect=None):
    begin = dt.now()
    packet_type = None
    metadata = None
    timeout_delta = timedelta(seconds=timeout)

    while True:
        try:
            data = inq.get(True,.05)
            if (len(data) > 0):
                # Data is a tuple of either:
                #    (valid,   list of parsed values)
                #    (invalid, string)
                if data[0]:

                    if debug:
                        print('dbg<-: ', data)

                    # If the response is a valid firmware update response
                    # then get what it is and any metadata
                    packet_type = data[1][0]
                    metadata = list()

                    # Anything beyond the packet type is packet metadata or
                    # response fields
                    if len(data[1]) > 1:
                        # if its a maufacturing command reponse then split those
                        # items further based on whitespace
                        if data[1][1].startswith(MFG_CMD_RESPONSE):
                            mfg_resp_items = data[1][1].split()
                            metadata.extend(mfg_resp_items)
                        else:
                            metadata.append(data[1][1])

                    if len(data[1]) >= 2:
                        # if it has meta data beyond the 1st item
                        # then get that too
                        metadata.extend(data[1][2:])

                    if packet_type in [WUFF_Q_CONSOLE, WUFF_Q_CONSOLE_BUG]:
                        if data[1][1].startswith(MFG_CMD_RESPONSE):
                            packet_type = MFG_RESPONSE_TYPE
                        else:
                            packet_type = CONSOLE_TYPE
                else:
                    # If the packet is not a valid response but
                    # has data then it's a gen2/3 console message

                    packet_type = 'CONSOLE'
                    metadata = data[1].strip()

                if packet_type in [CONSOLE_TYPE, MFG_RESPONSE_TYPE]:
                    display_data = False

                    if (packet_type == CONSOLE_TYPE) and show_console:
                        display_data = True

                    if (packet_type == MFG_RESPONSE_TYPE) and debug:
                        display_data = True

                    if display_data:
                        if type(data[1]) is list:
                            print('{}: {}'.format(packet_type, data[1][1].rstrip()))
                        else:
                            print('{}: {}'.format(packet_type, data[1].rstrip()))

                if (expect is None) and (q_expect is None):
                    break
                else:
                    if q_expect is not None:
                        if packet_type in q_expect:
                            break

                    if expect is not None:
                        # If the string matches what we are looking for
                        # then stop.  Otherwise keep looking
                        indata = None

                        # If the metadata is a list then there was
                        # input packet type match.  It's unlikely that the
                        # expect value will match but in the WUFF case
                        # the console output data will be in
                        # the list after the prefix so try to extract
                        # that or use and empty string if it does not
                        # exist.

                        if type(metadata) is list:
                            indata = ''
                            if len(metadata) > 1:
                                indata = metadata[1]
                        else:
                            indata = metadata

                        if indata.startswith(expect):
                            break

        except queue.Empty as e:
            elapsed = dt.now() - begin

            if timeout>0 and elapsed > timeout_delta:
                packet_type = 'TIMEOUT'
                metadata = None
                break

            continue

        except:
            raise

    return (packet_type,metadata)

def wait_for_ACK_or_NAK(inq,timeout=0, console=True, wuff=False):
    begin = dt.now()
    timeout_delta = timedelta(seconds=timeout)
    while True:
        result = wait_for_response(inq, .1, show_console=console)

        if wuff:
            wuff_response = False

            # Translate WUFF response codes into result codes that
            # work with how the gen2/gen3 responses were handled
            # but with new keyword codes

            if result[0] == WUFF_Q_FW_RESP:

                wuff_response = True
                fw_resp_result = int(result[1][WUFF_FWRESP_RESULT_LOC])
                fw_resp_data = result[1]
                mapped_result = None

                if fw_resp_result == WUFF_FWRESP_SUCCESS:
                    mapped_result = ACK
                elif fw_resp_result == WUFF_FWRESP_TOOSMALL:
                    mapped_result = TOOSMALL
                elif fw_resp_result == WUFF_FWRESP_UNSTUFF_FAIL:
                    mapped_result = UNSTUFF
                elif fw_resp_result == WUFF_FWRESP_CRCFAIL:
                    mapped_result = CRCFAIL
                elif fw_resp_result == WUFF_FWRESP_PROGFAIL:
                    mapped_result = PROGFAIL
                else:
                    mapped_result = UNHNANDLED_WUFF

            if wuff_response:
                # Recreate the response tuple with translated
                # values
                result = (mapped_result, fw_resp_data)
                break;

        else:
            if (   (result[0] == ACK)
                or (result[0] == NAK)
                or (result[0] == CHKFAIL)):

                break

        elapsed = dt.now() - begin

        if timeout>0 and elapsed > timeout_delta:
            result = (TIMEOUT,None)
            break

    return result

def do_fsync(comm,retries=10):
    print('Sending sync')

    cmd_retries = retries

    while cmd_retries > 0:
        got_ack = False
        comm.q_puts('FSYNC\n')
        try:
            ack_waits = 2
            while ack_waits > 0:
                result = wait_for_response(comm.inq, timeout=.3, show_console=debug)

                if result[0] == ACK:
                    got_ack = True
                    break;

                ack_waits-=1

        except KeyboardInterrupt:
            raise KeyboardInterrupt

        if got_ack:
            break

        cmd_retries -= 1

    # Eat anything left in the buffer
    console_count = 0
    while True:
        result = wait_for_response(comm.inq, timeout=1, show_console=debug)

        if result[0] == TIMEOUT:
            break
        elif result[0] == CONSOLE:
            # If the console is generating constant data
            # then don't hang waiting for it to stop
            console_count += 1

            if console_count >= 5:
                break;

    return got_ack

def comm_puts_wrapper(comm, xmit_out, show_xmit=False):

    if show_xmit:
            print(f'dbg->: {xmit_out.strip()}')

    comm.q_puts(xmit_out)

def send_with_bytestuff(inbytes, q=None, wuff=False):
    if q is None:
        return

    # Looping thought the data twice isn't most
    # efficient thing but the serial thread wants
    # things in iterable chunks.  So build up
    # a new bytearray with the bytes stuffed

    ESC_BYTE = 0x7d
    outdata = bytearray()
    checksum = 0

    reserved_bytes = [ESC_BYTE,0x0a,0x0d]
    for each in inbytes:
        checksum += each
        if each in reserved_bytes:
            outdata.append(ESC_BYTE)
            outdata.append(each ^ 0x20)
        else:
            outdata.append(each)

    checksum = (checksum ^ 0xff) & 0xff

    if not wuff:
        ending = bytearray([checksum])

        for each in ending:
            if each in reserved_bytes:
                outdata.append(ESC_BYTE)
                outdata.append(checksum ^ 0x20)
            else:
                outdata.append(checksum)

    # Use 0x0a for line ending
    outdata.append(0x0a)

#    print("out:")
#    hdump(outdata)

    q.put(outdata)

def set_default_settings(settings=None):

    if settings is None:
        settings = dict()

    settings['binfile_fh'] = settings.get('binfile_fh', None)
    settings['do_sync'] = settings.get('do_sync', False)
    settings['byte_delay'] = settings.get('byte_delay', 0)
    settings['adaptive_byte_delay'] = settings.get('adaptive_byte_delay', False)
    settings['debug'] = settings.get('debug', False)
    settings['do_updt'] = settings.get('do_updt', False)
    settings['number_of_retries'] = settings.get('number_of_retries', 0)
    settings['max_send_retries'] = settings.get('max_send_retries', DEFAULT_SEND_RETRIES)
    settings['allow_charging'] = settings.get('allow_charging', False)

    return settings

def detect_harvard(comm, retries=4):

    cmd_retries = retries

    # Look for Harvard device with WUFF
    while cmd_retries > 0:
        got_ack = False
        comm_puts_wrapper(comm, WUFF_Q_OUT_SYNC, show_xmit=debug)

        try:
            q_rx = [WUFF_Q02]
            result = wait_for_response(comm.inq, timeout=.3, show_console=debug, q_expect=q_rx)

            if result[0] == WUFF_Q02 and result[1][0] == WUFF_Q_ACK:
                got_ack = True
                break;

        except KeyboardInterrupt:
            raise KeyboardInterrupt

        if got_ack:
            break

        cmd_retries -= 1

    return got_ack

def detect_gen4_bpk(comm, retries=4):

    cmd_retries = retries

    # If the the strap is gnerating a lot of console ouput then there can be many lines
    # of output buffered up. Try to drain that by sending a blank line and watching for the
    # prompt response.

    comm.q_puts(WUFF_Q_OUT_EMPTY)
    wait_for_response(wsc.inq, timeout=.1, show_console=debug, expect=WUFF_Q_PROMPT)

    # Look for Harvard device with WUFF
    while cmd_retries > 0:
        got_ack = False
        comm.q_puts(WUFF_X_OUT_SYNC)
        try:
            q_rx = [WUFF_X02]
            result = wait_for_response(comm.inq, timeout=.2, show_console=debug, q_expect=q_rx)

            if result[0] == WUFF_X02 and result[1][0] == WUFF_X_ACK:
                got_ack = True
                break;

        except KeyboardInterrupt:
            raise KeyboardInterrupt

        if got_ack:
            break

        cmd_retries -= 1

    return got_ack

def detect_strap_nfc(comm, retries=4, show_xmit=False):

    cmd_retries = retries

    # If the the strap is gnerating a lot of console ouput then there can be many lines
    # of output buffered up. Try to drain that by sending a blank line and watching for the
    # prompt response.

    if show_xmit:
        print(f'dbg->: \n')

    comm.q_puts(WUFF_Q_OUT_EMPTY)
    wait_for_response(wsc.inq, timeout=.1, show_console=debug, expect=WUFF_Q_PROMPT)

    # Look for Harvard device with WUFF
    while cmd_retries > 0:
        got_ack = False
        xmit_out = WUFF_W_OUT_SYNC

        if show_xmit:
            print(f'dbg->: {xmit_out.strip()}')

        comm.q_puts(xmit_out)

        try:
            q_rx = [WUFF_W02]
            result = wait_for_response(comm.inq, timeout=.2, show_console=debug, q_expect=q_rx)

            if result[0] == WUFF_W02 and result[1][0] == WUFF_W_ACK:
                got_ack = True
                break;

        except KeyboardInterrupt:
            raise KeyboardInterrupt

        if got_ack:
            break

        cmd_retries -= 1

    return got_ack

def send_wuff_start_load(comm):
    got_ack = False
    pkt_rev = None
    chunk_size = None

    comm.q_puts('Q05,1,3,83FA\n')
    try:
        q_rx = [WUFF_Q_CMD_RESP]
        result = wait_for_response(comm.inq, timeout=2, q_expect=q_rx)

        if result[1] is not None:
            if (    int(result[1][WUFF_CMDRESP_CMD_LOC]) == WUFF_CMD_START_FW
                and int(result[1][WUFF_CMDRESP_RESULT_LOC]) == WUFF_CMDRESP_SUCCESS):
                    pkt_rev  = int(result[1][3])
                    chunk_size = int(result[1][4])
                    print('Start OK. Pkt Rev %d, Chunk size %d' % (pkt_rev,chunk_size))
                    got_ack = True
            else:
                pass

    except KeyboardInterrupt:
        raise KeyboardInterrupt

    return (got_ack,pkt_rev,chunk_size)

def send_wuff_end_load(comm):
    got_ack = False
    comm.q_puts('Q05,2,4,F7F8\n')
    try:
        q_rx = [WUFF_Q_CMD_RESP]
        result = wait_for_response(comm.inq, timeout=.5, q_expect=q_rx)

        if result[0] != TIMEOUT:
            if int(result[1][WUFF_CMDRESP_CMD_LOC]) == WUFF_CMD_END_FW:
                if int(result[1][WUFF_CMDRESP_RESULT_LOC]) == WUFF_CMDRESP_SUCCESS:
                    got_ack = True

                if int(result[1][WUFF_CMDRESP_RESULT_LOC]) == WUFF_CMDRESP_FAILURE:
                    got_ack = False

    except KeyboardInterrupt:
        raise KeyboardInterrupt

    return (got_ack,result)

def send_wuff_reboot(comm):
    got_ack = False
    print("Sending reboot cmd")
    comm.q_puts('Q05,3,2,ABFA\n')
    try:
        q_rx = [WUFF_Q_CMD_RESP]
        result = wait_for_response(comm.inq, timeout=.5, q_expect=q_rx)

        if result[0] != TIMEOUT:
            if int(result[1][WUFF_CMDRESP_CMD_LOC]) == WUFF_CMD_REBOOT:
                if int(result[1][WUFF_CMDRESP_RESULT_LOC]) == WUFF_CMDRESP_SUCCESS:
                    got_ack = True

    except KeyboardInterrupt:
        raise KeyboardInterrupt

    return got_ack

def send_wuff_process_image(comm):
    got_ack = False
    print("Sending process image cmd")
    comm.q_puts('Q05,4,5,EFF9\n')
    try:
        q_rx = [WUFF_Q_CMD_RESP]
        result = wait_for_response(comm.inq, timeout=.5, q_expect=q_rx)

        if result[0] != TIMEOUT:
            if int(result[1][WUFF_CMDRESP_CMD_LOC]) == WUFF_CMD_REBOOT:
                if int(result[1][WUFF_CMDRESP_RESULT_LOC]) == WUFF_CMDRESP_SUCCESS:
                    got_ack = True

    except KeyboardInterrupt:
        raise KeyboardInterrupt

    return got_ack

def send_gen2_gen3_start_load(comm):
    got_ack = False
    chunk_size = 0

    comm.q_puts('FIRM\n')
    try:
        result = wait_for_ACK_or_NAK(comm.inq,timeout=15)

        if result[0] == ACK:
            if result[1][0] == 'ERASEOK':
                chunk_size = int(result[1][1])
                print('Erase OK. Chunk size %d' % chunk_size)
                got_ack = True
        elif result[0] == NAK:
            print("Firmware update start error %s" % result[1])
        else:
            pass

    except KeyboardInterrupt:
        raise KeyboardInterrupt

    return (got_ack,chunk_size)

def modbus_crc(data):
    data = bytearray(data)
    poly = 0xA001
    crc = 0xFFFF
    for b in data:
        crc ^= (0xFF & b)
        for _ in range(0, 8):
            if (crc & 0x0001):
                crc = ((crc >> 1) & 0xFFFF) ^ poly
            else:
                crc = ((crc >> 1) & 0xFFFF)

    result = struct.pack('>H',crc)
    return result

def get_bootlader_prompt(comm):
    # Line feeds to get the bootloader prompt
    for loop in range(5):
        comm.q_puts('\n')
        response = wait_for_response(wsc.inq, timeout=1, show_console=True, expect='>>')

        if response[0] != TIMEOUT:
            break;

def send_gen4_batpack_field_state(comm, state, show_xmit=False):
    got_ack = False
    xmit_out = ''

    # 'on' is pulse charging disabled (mode 4) which allows for persistent NFC
    # communication though the battery pack to the strap if its not 'on' then
    # revert back to pulse charging enabled (mode 3).

    if state == NFC_FIELD_ON:
        xmit_out = WUFF_X_NFC_MODE4
    else:
        xmit_out = WUFF_X_NFC_MODE3

    if show_xmit:
            print(f'dbg->: {xmit_out.strip()}')

    comm.q_puts(xmit_out)

    try:
        q_rx = [WUFF_X_CMD_RESP]
        result = wait_for_response(comm.inq, timeout=.5, q_expect=q_rx)

        if result[0] != TIMEOUT:
            if int(result[1][WUFF_CMDRESP_CMD_LOC]) == WUFF_BPKCMD_SET_FMODE:
                if int(result[1][WUFF_CMDRESP_RESULT_LOC]) == WUFF_CMDRESP_SUCCESS:
                    got_ack = True

                if int(result[1][WUFF_CMDRESP_RESULT_LOC]) == WUFF_CMDRESP_FAILURE:
                    got_ack = False

    except KeyboardInterrupt:
        raise KeyboardInterrupt

    return (got_ack, result)

def send_set_strap_charging_state(comm, state, retries=4, show_xmit=False, xmit_out=None):
    cmd_retries = retries

    while cmd_retries > 0:
        got_ack = False

        # 'on' is pulse charging disabled (mode 4) which allows for persistent NFC
        # communication though the battery pack to the strap if its not 'on' then
        # revert back to pulse charging enabled (mode 3).

        if state == STATE_ENABLED:
            xmit_out = MFG_CMD_CHARGING_ENABLE
        else:
            # The m087 charging disable behavior changed to timeout in 10 seconds
            # a new m091 command was added that disables until re-enabled or the strap
            # reboots. Which of the commands to send for a disable can be
            # specified as an optional arguement.  Default is to use
            # the m91 command

            if xmit_out is None:
                xmit_out = MFG_CMD_CHARGING_DISABLE

        comm_puts_wrapper(comm, xmit_out, show_xmit=show_xmit)

        try:
            q_rx = [MFG_RESPONSE_TYPE]
            result = wait_for_response(comm.inq, timeout=.5, show_console=debug, q_expect=q_rx)

            if result[0] != TIMEOUT:
                if (result[1][WUFF_MFGRESP_RESULT_LOC] == MFG_CMD_PASS):
                    got_ack = True
                else:
                    got_ack = False

        except KeyboardInterrupt:
            raise KeyboardInterrupt

        if got_ack:
            break

        cmd_retries -= 1

    return (got_ack, result)

def send_set_strap_quiet_state(comm, state, retries=4, show_xmit=False):
    xmit_out = ''
    cmd_retries = retries

    while cmd_retries > 0:
        got_ack = False

        if state == STATE_ENABLED:
            xmit_out = MFG_CMD_ENABLE_QUIET_MODE
        else:
            xmit_out = MFG_CMD_DISABLE_QUIET_MODE

        if show_xmit:
                print(f'dbg->: {xmit_out.strip()}')

        comm.q_puts(xmit_out)

        try:
            q_rx = [MFG_RESPONSE_TYPE]
            result = wait_for_response(comm.inq, timeout=.5, show_console=debug, q_expect=q_rx)

            if result[0] != TIMEOUT:
                if (result[1][WUFF_MFGRESP_RESULT_LOC] == MFG_CMD_OK):
                    got_ack = True
                else:
                    got_ack = False

        except KeyboardInterrupt:
            raise KeyboardInterrupt

        if got_ack:
            break

        cmd_retries -= 1

    return (got_ack, result)


def main(wscomm, settings):

    wsc = wscomm

    wsc.attach_threads()
    wsc.start_threads()

    outq = wsc.get_write_queue()
    inq  = wsc.get_read_queue()

    byte_delay = settings['byte_delay']
    adaptive_byte_delay = settings['adaptive_byte_delay']
    debug = settings['debug']
    do_updt = settings['do_updt']
    number_of_retries = settings['number_of_retries']

    wsc.set_byte_delay(byte_delay)

    try:
        if settings['do_sync']:
            result = do_fsync(wsc,retries=20)

            if result:
                print('Synced')
            else:
                print('Sync failed')

        # This is a generic place holder for testing
        # various methods that send data to
        # the strap.
        if settings['run_test']:
            # Wake up strap
            wsc.q_puts('\n\n')
            print('Running dev test')
            send_wuff_reboot(wsc)
            print('Waiting for bootloader')
            wait_for_response(wsc.inq, timeout=15, show_console=True, expect='Entering bootloader console')

        binfile_fh = settings['binfile_fh']

        keep_going              = True
        got_bracelet_response   = False

        if binfile_fh is None:
            keep_going = False
        else:
            # Slurp in the file.  Files are not currently large enough to
            # worry about ram usage so just load the whole file

            upgrade_data = bytearray(binfile_fh.read())

        download_count = 0

        while keep_going:

            # Kick out a blank line to delineate the output when looping.
            # Helps for visual parsing if run multiple times or if
            # infinite mode is used.

            print('')

            gen4_strap = False
            gen4_bpk = False

            # Start off assuming WUFF is avaiable
            use_wuff = True

            nfc_up = False
            result = False

            if settings['no_wuff']:
                use_wuff = False
            else:
                print('Checking for gen4 BPK')
                gen4_bpk = detect_gen4_bpk(wsc)

            if gen4_bpk:
                print('Found gen4 battery pack. Setting feild on')
                cmd_ok, cmd_result = send_gen4_batpack_field_state(wsc, NFC_FIELD_ON, show_xmit=debug)

                if not cmd_ok:
                    print('Field on cmd failed with {}'.format(cmd_result))
                else:
                    # If the feild was turned on then the battery pack and
                    # strap combo needs some time to enable the communication
                    # path

                    time.sleep(1.0)

                    # Disable quiet mode
                    print('Disabling quiet mode')
                    cmd_ok, cmd_result = send_set_strap_quiet_state(wsc, STATE_DISABLED, show_xmit=debug)

                    if cmd_ok:
                        print('Quiet mode disabled'.format(cmd_result))
                    else:
                        print('Disable quiet mode cmd failed with {}'.format(cmd_result))

            else:
                print('No battery pack found')

            if not settings['no_wuff']:
                print('Checking for a strap with NFC')
                nfc_up = detect_strap_nfc(wsc, show_xmit=debug)

            if nfc_up:
                print('Found strap with a NFC interface')
            else:
                print('No strap NFC interface detected')

            if not settings['no_wuff']:
                print('Checking for WUFF protocol')
                use_wuff = detect_harvard(wsc)

            if use_wuff:
                print('Got a Q sync. Using WUFF protocol')
                result = True
            else:
                print('Using legacy non-WUFF protocol.')

            if use_wuff:
                if not settings['allow_charging']:
                    print('Disabling strap battery charging')

                    # The charging disable process changes based on what
                    # firmware version is running
                    # Start with the newer command

                    cmd_ok, cmd_result = send_set_strap_charging_state(
                                            wsc,
                                            STATE_DISABLED,
                                            xmit_out=MFG_CMD_CHARGING_DISABLE,
                                            show_xmit=debug)

                    if not cmd_ok:
                        # Fallback to the previous command.
                        cmd_ok, cmd_result = send_set_strap_charging_state(
                                                wsc,
                                                STATE_DISABLED,
                                                xmit_out=MFG_CMD_OLD_CHARGING_DISABLE,
                                                show_xmit=debug)

                    if not cmd_ok:
                        print('Unable to disable charging')

            if not use_wuff:
                print('Sending FSYNCs trying to get the bracelets attention')
                result = do_fsync(wsc, retries=50)

                response = None

                if not result:
                    print('No FSYNC response. Checking mode.')
                    # Check to see if we are running early gen3 code that
                    # Needs to be rebooted into the bootloader
                    wsc.q_puts('v\n')

                    response = wait_for_response(wsc.inq, timeout=.2, show_console=debug, expect='Ver:')

                    if response[0] == CONSOLE:
                            print('Strap has app running but FSYNC fails. Rebooting to bootloader')

                            # Reboot the strap into the bootloader
                            wsc.q_puts('K\n');
                            continue
                    else:
                        result = False
                else:
                    print("Got a FSYNC response. Starting download")

            chkfail_count   = 0
            progfail_count  = 0
            unknown_count   = 0
            chunk_size      = 0
            crcfail_count   = 0
            timeout_count   = 0

            if result:
                cmd_retries = 2
                while cmd_retries > 0:
                    got_ack = False
                    print("Sending starting firmware load")

                    # Start the firmware upgrade process
                    if use_wuff:
                        got_ack, pkt_rev, chunk_size = send_wuff_start_load(wsc)
                    else:
                        got_ack, chunk_size = send_gen2_gen3_start_load(wsc)

                    if got_ack:
                        break

                    print("Retrying start firmware update")
                    cmd_retries -= 1

                if got_ack:

                    data_size = len(upgrade_data)

                    if data_size % chunk_size:
                        # Not a multiple of chunk_size
                        extra = ((int(data_size/chunk_size)+1)*chunk_size) - data_size
                        upgrade_data.extend(bytearray(extra))
                        data_size = len(upgrade_data)

                    print("Sending %d bytes %d chunks" % (data_size,(data_size/chunk_size)))

                    chunk_offset = 0
                    loop_count = 0
                    start_time  = dt.now()

                    while chunk_offset < data_size:
                        chunk = upgrade_data[chunk_offset:chunk_offset+chunk_size]
                        pkt_data = bytearray()

                        # WUFF needs a prefix added
                        if use_wuff:
                            pkt_data.extend(WUFF_Q_FW_PREFIX.encode('utf-8'))

#                        pkt_data.extend(b'\x04\x03\x02\x01')
#                        pkt_data.extend(b'\x05\x06\x07\x08')

                        pkt_data.extend(struct.pack('>L',chunk_offset))
                        pkt_data.extend(chunk)

                        # WFF needs CRC16 at the end
                        if use_wuff:
                            pkt_data.extend(modbus_crc(pkt_data))

                        send_retries = settings['max_send_retries']

                        while send_retries > 0:
                            got_ack = False

                            pct_done = (float(chunk_offset)/float(data_size)) * 100.0

                            end_time = dt.now()

                            elapsed_minutes =  (end_time - start_time).total_seconds() / 60.0

                            print("Elapsed: %.3f minutes: Sending Offset: %08x %.1f%% " %
                                    (elapsed_minutes, chunk_offset, pct_done), end='')

                            send_with_bytestuff(pkt_data, q=outq, wuff=use_wuff)

                            show_console = debug or settings['show_console']

                            response = wait_for_ACK_or_NAK(inq, timeout=2, console=show_console, wuff=use_wuff)

                            if response[0] == ACK:
                                got_ack = True
                            else:
                                # Add a time delta the following error messages.
                                end_time = dt.now()
                                elapsed_minutes =  (end_time - start_time).total_seconds() / 60.0
                                print("\nElapsed: %.3f minutes: " % (elapsed_minutes), end='')

                                if (response[0] == NAK) or (response[0] == CRCFAIL):

                                    # Keep track of the number of errors and the type
                                    # received back
                                    # WUFF reponse codes are decimal number strings so try to
                                    # convert those to numbers
                                    try:
                                        wuff_response_code = int(response[1][0])
                                    except:
                                        # Zero is unused
                                        wuff_response_code = 0

                                    if response[1][0] == CHKFAIL:
                                        chkfail_count += 1

                                        if adaptive_byte_delay:
                                            byte_delay += 50
                                            wsc.set_byte_delay(byte_delay)

                                    elif response[1][0] == PROGFAIL:
                                        progfail_count += 1

                                    elif (response[1][0] == CRCFAIL) or (wuff_response_code == WUFF_FWRESP_CRCFAIL):
                                        crcfail_count += 1
                                    else:
                                        print('Unknown response code: ', response[1][0])
                                        unknown_count += 1

                                    print("Firmware write chunk error %s @ %08x" % (response[1], chunk_offset))
                                    print("    Byte delay: %u" % byte_delay)
                                    print("%8s count: %d" % (CHKFAIL, chkfail_count))
                                    print("%8s count: %d" % (PROGFAIL, progfail_count))

                                    # Only stall if the error was not a checksum failure
                                    # The stall is so unexpected errors can be seen eaiser
                                    # when the transfer is in progress.

                                    if response[1][0] != CHKFAIL:
                                        response = wait_for_ACK_or_NAK(inq, timeout=2)

                                elif response[0] == TIMEOUT:
                                    print('Response timeout')
                                    timeout_count += 1

                                else:
                                    print('Unknown response: ', response)

                            if (got_ack):
                                chunk_offset += chunk_size
                                print("\r", end='')
                                break
                            else:
                                print("Retrying offset @ %08x" % (chunk_offset))
                                send_retries -= 1

                        # Unable to get a good data send
                        if send_retries == 0:
                            print('Retry limit reached. Aborting')
                            break

                    print()

                    end_time = dt.now()

                    print('Duration: %.1f minutes' % ((end_time - start_time).total_seconds() / 60.0))

                    download_count += 1

                    image_verified = False

                    if use_wuff:
                        print("Sending FW end command")
                        image_verified, response = send_wuff_end_load(wsc)
                    else:
                        print("Sending break for image verify")
                        wsc.send_break()
                        response = wait_for_ACK_or_NAK(inq,timeout=5)
                        if response[0] == ACK:
                            image_verified = True

                    print('Error summary:')
                    print(' Attempt %d' % download_count)
                    print(' %d %s Errors' % (chkfail_count, CHKFAIL))
                    print(' %d %s Errors' % (progfail_count, PROGFAIL))
                    print(' %d %s Errors' % (timeout_count, TIMEOUT))
                    print(' %d %s Errors' % (unknown_count, 'Unknown'))

                    if image_verified:

                        print("Firmware update image CRC passed")
                        keep_going = False

                        if use_wuff:
                            if settings['reboot']:
                                send_wuff_reboot(wsc)
                            else:
                                send_wuff_process_image(wsc)
                        else:
                            if do_updt:
                                print("Sending 'updt' bootloader command")
                                wsc.q_puts('updt\n')

                        # Wait for the bootloader to come back.
                        print('Waiting for the bootloader')
                        wait_for_response(wsc.inq, timeout=15, show_console=True, expect='Entering bootloader console')

                        get_bootlader_prompt(wsc)

                        # Tell the bootloader to show the versions
                        print("Sending 'vers' bootloader command")
                        wsc.q_puts('vers\n')
                        wait_for_response(wsc.inq, timeout=1, show_console=True, expect='Update Version:')

                        get_bootlader_prompt(wsc)

                        # Tell the bootloader to continue
                        print("Sending 'cont' bootloader command")
                        wsc.q_puts('cont\n')

                        # Wait for the app to boot
                        wait_for_response(wsc.inq, timeout=10, show_console=True, expect='Debug console active')

                        if settings['infinite']:
                            print('Waiting for strap removal')
                            strap_present = True

                            while strap_present:
                                strap_present = detect_harvard(wsc)

                            print('Strap removed. Starting over')
                    else:
                        print("Firmware update image check failed: %s" % response[1])

                        # If infinite mode is set then loop over and over looking for a strap
                        if not settings['infinite']:
                            if download_count <= number_of_retries:
                                print("Attempt %d of %d. Trying again." % (download_count, number_of_retries+1))
                            else:
                                print('Transfer retry count reached. Stopping')
                                keep_going = False
                        else:
                            print('Infinite mode enabled. Starting over.')

            else:
                print("Can't seem to get a response from the bracelet.")

                download_count += 1

                if not settings['infinite']:
                    if download_count <= number_of_retries:
                        print("Attempt %d of %d. Trying again." % (download_count, number_of_retries+1))
                    else:
                        print('Transfer retry count reached. Stopping')
                        keep_going = False

                else:
                    print('Infinite mode enabled. Starting over.')

    except KeyboardInterrupt:
        print('Key exit')
    except:
        print("Unexpected error:", sys.exc_info()[0])
        raise
    finally:
        if use_wuff:
            send_set_strap_charging_state(wsc, STATE_ENABLED, show_xmit=debug)

        if gen4_bpk:
            print('Restoring NFC field mode to pulse charging')
            cmd_ok, cmd_result = send_gen4_batpack_field_state(wsc, NFC_FIELD_PULSE)

            if not cmd_ok:
                print('Field mode pulse cmd failed with {}'.format(cmd_result))

        wsc.send_break()
        wsc.stop_threads()

if __name__ == '__main__':

    parser = argparse.ArgumentParser(description='WhoopStrap serial firmware upgrade')

    parser.add_argument('binfile',
                            action='store', nargs='?',
                            type=argparse.FileType('rb'), default=None,
                            help='Name of .bin file to program')
    parser.add_argument('-p','--port',
                            nargs=1, default = None,
                            help='use comport <port>')
    parser.add_argument('-u','--update',action='store', type=argparse.FileType('rb'),
                            default=None,
                            help="Deprecated legacy compatibilty option")

    parser.add_argument('-S','--sync', action='store_true',default=False,
                            help='Send a sync command')

    parser.add_argument('--debug', action='store_true',default=False,
                            help='Enable debug output')

    parser.add_argument('--updt', action='store_true', default=False,
                            help='Issue bootloader updt command after download')

    parser.add_argument('-r', '--retries', action='store', default=0, type=int,
                            help='Number of times to retry if CRC32 fails')

    parser.add_argument('-V', '--version', action='store_true', default=False,
                            help='Show version info')

    parser.add_argument('-b', '--byte-delay', action='store', default=None, type=int,
                            nargs='?', const=-1,
                            help='Number of us to delay between each sent byte')

    parser.add_argument('--adapt-delay', action='store_true', default=False,
                            help='Force byte delay to be adaptive')

    parser.add_argument('-L', '--list-ports', action='store_true', default=False,
                            help='List available comm ports')

    parser.add_argument('--test', action='store_true',default=False,
                            help='Internal developement testing')

    parser.add_argument('--no-wuff', action='store_true',default=False,
                            help='Do not probe for WUFF support')

    parser.add_argument('-2', '--twostops', action='store_true', default=False,
                            help='Enable 2 stopbits workround for framing errors')

    parser.add_argument('-1', '--onestop', action='store_true', default=False,
                            help='Use 1 stop bit in UART communcation')

    parser.add_argument('-R','--reboot', action='store_true', default=False,
                            help='Force a reboot after successful image validation')

    parser.add_argument('-C','--console', action='store_true', default=False,
                            help='Enable display of strap console messages during download')

    parser.add_argument('--send-retries', action='store', default=DEFAULT_SEND_RETRIES, type=int,
                            help='Number of times to retry a packet before aborting')

    parser.add_argument('-I', '--infinite', action='store_true', default=False,
                            help='Loop endlessly tyring to look for a strap to update')

    parser.add_argument('--allow-charging', action='store_true', default=False,
                            help='Do not send the charging disable command before image transfer')

    args = parser.parse_args()

    wsc = WS_comm_threaded()

    port_opened = False

    debug = args.debug

    do_updt = args.updt

    number_of_retries = args.retries

    # Default adaptive to disabled
    # and no byte delays
    adaptive_byte_delay = False
    byte_delay          = 0

    # Byte delay has 2 modes of operation depeding on the arguments specified.
    # If -b, --byte-delay is used without an argument then adaptive mode is
    # used with the default delay.
    # If -b, --byte-delay is used with an arguement then fixed mode used
    # and the byte delay is the argument in microseconds and will not
    # auto-increment on chksum errors.
    # Specfying a fixed byte delay of zero is the same as disabled. No byte
    # delay is used.

    if args.byte_delay is not None:

        if args.byte_delay == -1:
            # Start at a 100 us byte delay
            # and increase for each CHKFAIL
            adaptive_byte_delay = True
            byte_delay          = 100
        else:
            adaptive_byte_delay = False
            byte_delay          = args.byte_delay

    # If the force adaptive option specified then always
    # adapte the delay regardless if a value was specified
    # Useful for creating a higher starting value for the
    # delay yet allowing for increase if it still fails.

    if args.adapt_delay:
            adaptive_byte_delay = True

    if args.version:
        if version_extn is None:
            print('Version: %s.%s.%s' % (version_major, version_minor, version_point))
        else:
            print('Version: %s.%s.%s-%s' % (version_major, version_minor, version_point, version_extn))

    if args.list_ports:
        print('Available ports:')
        wsc.list_ports()

    # Work around what seems to be a false framing error reported by Maxims UART bridge
    # Using 2 stop bits while the Maxim is set to 1 stop bit adds a one bittime delay
    # to each byte and this seems to help prevent errors seen when sending image
    # data files to the WhoopStrap.

    wsc.set_use_twostops(args.twostops)

    # If one stop is set then override two stops and use 1 stop bit
    # This is in anticipation of two stop bits becoming the default

    if args.onestop:
        wsc.set_use_twostops(False)

    if wsc.get_use_twostops():
        print('Using two stopbits framing error workaround')
        f = open("script_start.txt", "a")
        f.write("Start!")
        f.close()
    else:
        print('Using one stopbit. Framing error workaround disabled')

    if args.port is not None:
        if len(args.port) > 0:
            for each in args.port:
                print(each)
                port_opened = wsc.open(device=each)
                if port_opened:
                    print("Opened device %s" % each)
                    break;
        else:
            port_opened = wsc.open()

    if not port_opened:
        if not args.list_ports:
            print("Can't open any serial ports")
            print('Avaliable ports:')
            wsc.list_ports()

        sys.exit(1)

    if adaptive_byte_delay:
        print('Adaptive byte delay starting at: %u' % byte_delay)
    else:
        if byte_delay == 0:
            print('Byte delay not active')
        else:
            print('Fixed byte delay: %u' % byte_delay)

    # The settings dictionary contains the global settings
    # for the program. Set up the defaults

    settings = set_default_settings()

    # Change the settings based on the arguements provided
    settings['binfile_fh'] = args.binfile
    settings['do_sync'] = args.sync
    settings['byte_delay'] = byte_delay
    settings['adaptive_byte_delay'] = adaptive_byte_delay
    settings['debug'] = args.debug
    settings['do_updt'] = args.updt
    settings['number_of_retries'] = args.retries
    settings['run_test'] = args.test
    settings['no_wuff'] = args.no_wuff
    settings['twostops'] = args.twostops
    settings['reboot'] = args.reboot
    settings['show_console'] = args.console
    settings['max_send_retries'] = args.send_retries
    settings['infinite'] = args.infinite
    settings['allow_charging'] = args.allow_charging

    # Attempt to update
    main(wsc, settings)
