#!/usr/bin/python 
#coding=utf-8

'''
自动化签名和打OBB版本
'''

import os, sys, platform
import  subprocess
import shutil
import time
import win_utf8_output

def AutoSign():
    keystorePath = "";
    while True:
        s = raw_input("\n请输入.keystore的文件路径：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        keystorePath = s;
        break;

    keystoreAlias = "";
    while True:
        s = raw_input("\n请输入keystore的alias：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        keystoreAlias = s;
        break;

    keystorePassword = "";
    while True:
        s = raw_input("\n请输入keystore的密码：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        keystorePassword = s;
        break;

    unsignPath = "";
    while True:
        s = raw_input("\n请输入待签名的apk文件路径(文件为jar，请自行ZIP工具压缩生成jar)：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        unsignPath = s;
        break;

    signPath = "";
    while True:
        s = raw_input("\n请输入签名后生成的apk文件路径：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        signPath = s;
        break;

    cmd = "jarsigner -verbose -keystore %s -storepass %s -signedjar %s %s %s" % \
          (keystorePath, keystorePassword, signPath, unsignPath, keystoreAlias);

    print cmd;

    os.system(cmd);

    return

def BuildObb():

    fileDir = "";
    while True:
        s = raw_input("\n请输入源目录路径：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        fileDir = s;
        break;

    outDir = "";
    while True:
        s = raw_input("\n请输入产生路径：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            outDir = os.path.dirname(os.path.realpath(fileDir));
        else:
            outDir = os.path.realpath(s);
        break;


    apkName = "";
    while True:
        s = raw_input("\n请输入Bundle Identifier(请看Unity里Android ProjectSetting里Bundle Identifier)：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        if (s.isdigit()):
            continue;
        apkName = s;
        break;

    patchName = "main"
    while True:
        s = raw_input("\n是否是主Patch(Y/N)：\n")
        if (s != None):
            s.strip();
        if s == 'y' or s == 'Y':
            patchName = "main";
            break
        elif s == 'n' or s == 'N':
            patchName = "patch";
            break;

    apkVersion = 0;
    while True:
        s = raw_input("\n请输入对应APK VersionCode(请看Unity里Android ProjectSetting里VersionCode)：\n")
        if (s != None):
            s.strip();
        if (s.isdigit()):
            apkVersion = int(s);
            break;

    obbFileName = "%s.%d.%s.obb" % (patchName, apkVersion,apkName);

    outFileName = outDir + "/" + obbFileName;

    cmd = "jobb -d %s -o %s -pn %s -pv %d" % (fileDir, outFileName, apkName, apkVersion);

    print cmd;

    os.system(cmd);

    return

def Main():
    while True:
        s = raw_input("\n请选择操作类型：1.自动签名 2.生成obb 3.退出\n")
        if (s.isdigit()):
            cmdId = int(s)
            if (cmdId in [1, 2, 3]):
                if (cmdId == 3):
                    break;
            if (cmdId == 1):
                AutoSign();
            elif (cmdId == 2):
                BuildObb();
    return;

##################################### 调用入口 ###################################
if __name__ == '__main__':
    Main()
#################################################################################
