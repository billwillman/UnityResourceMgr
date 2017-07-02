#! usr/bin/python #coding=utf-8

import os, sys

class ConfigFile:
    def __init__(self, fileName):
        self.fileName = fileName
        self.keyValue = {}
        return

    def LoadFromFile(self):
        fileName = self.fileName
        if (not os.path.exists(fileName)) or (not os.path.isfile(fileName)):
            return False
        file = open(fileName, "r")
        if file == None:
            return False
        content = file.read()
        self._LoadFromStr(content)
        file.close()
        return True

    def SaveToFile(self):
        return True

    def _LoadFromStr(self, str):
        if str == None:
            return
        str = str.strip();
        if str == "":
            return
        return


