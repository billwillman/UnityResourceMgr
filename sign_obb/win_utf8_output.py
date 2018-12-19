# -*- coding: utf-8 -*-
import sys
global IsUtf8Inited
IsUtf8Inited = False
global ConsoleWrite
ConsoleWrite = None

class UnicodeStreamFilter:
    def __init__(self, target):
        self.target = target
        self.encoding = 'utf-8'
        self.errors = 'replace'
        self.encode_to = self.target.encoding
    def write(self, s):
        global IsUtf8Inited
        orgS = s
        try:
            if type(s) == str:
                s = s.decode("utf-8")
            s = s.encode(self.encode_to, self.errors).decode(self.encode_to)
            self.target.write(s)
            IsUtf8Inited = True
        except:
            if not IsUtf8Inited:
                sys.stdout = ConsoleWrite
                print orgS
            else:
                print "Encoding Error~!"

ConsoleWrite = sys.stdout
if sys.stdout.encoding == 'cp936':
    sys.stdout = UnicodeStreamFilter(sys.stdout)

if __name__ == "__main__":
    a = "你好"
    b = u"你好"
    print a
    print b