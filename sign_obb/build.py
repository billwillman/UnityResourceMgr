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
import zipfile
import shutil

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

def getApkInfoFromLog(logFile):
    if (logFile == None or len(logFile) <= 0 or (not os.path.exists(logFile)) or os.path.isdir(logFile)):
        return None, None;
    f = open(logFile);
    readStr = f.read();
    f.close();

    tmplen = len("package: name='");
    index = readStr.index("package: name='");
    if (index < 0):
        return None, None;
    index += tmplen;
    readStr = readStr[index:len(readStr) - index];
    index = readStr.index("'");
    if (index < 0):
        return None, None;
    packageName = readStr[0:index];
    if (packageName != None):
        packageName.strip();

    readStr = readStr[index + 1:len(readStr) - index];
    tmplen = len("versionCode='");
    index = readStr.index("versionCode='");
    if (index < 0):
        return packageName, None;
    index += tmplen;
    readStr = readStr[index:len(readStr) - index];
    index = readStr.index("'");
    if (index < 0):
        return packageName, None;
    versionCodeStr = readStr[0:index];
    if (versionCodeStr != None):
        versionCodeStr.strip();
    if (not versionCodeStr.isdigit()):
        return packageName, None;
    versionCode = int(versionCodeStr);

    return  packageName, versionCode;

def buildFromApk():
    srcApkFile = "";
    while True:
        s = raw_input("\n请输入完整APK包文件路径：\n")
        if (s != None):
            s.strip();
        if (s== None or len(s) <= 0):
            continue;
        isApk = os.path.splitext(s)[-1].lower() == ".apk";
        if (not isApk):
            continue;
        srcApkFile = s;
        break;

    print "读取APK包信息..."
    logFile = os.path.dirname(os.path.realpath(__file__)) + "/aapt.txt";
    cmd = "aapt dump badging %s > %s" % (srcApkFile, logFile);
    os.system(cmd);

    # 从日志文件中读取包名和versionCode
    packageName, versionCode = getApkInfoFromLog(logFile);
    if (packageName == None or versionCode == None):
        print "获得APK信息失败~!"
        return

    print "读取APK信息完成..."

    while True:
        output = "\n【packageName】 %s 【versionCode】 %d 是否确认？(Y/N): " % (packageName, versionCode);
        s = raw_input(output);
        if s == 'y' or s == 'Y':
            break;
        elif s == 'n' or s == 'N':
            return;

    idx = srcApkFile.index('.');
    if (idx < 0):
        return;

    unzipDir = srcApkFile[0:idx];
    # 1.如果原来目录有文件删除掉所有文件
    if (os.path.exists(unzipDir) and os.path.isdir(unzipDir)):
        print "正在删除目录：%s" % unzipDir;
        shutil.rmtree(unzipDir)

    #2.解压APK
    print "%s 开始解压APK..." % srcApkFile
    f = zipfile.ZipFile(srcApkFile, 'r')
    for file in f.namelist():
        f.extract(file, unzipDir)
    print "解压APK完成..."
    #3.assets/Android目录打OBB
    #4.删除assets/Android目录
    #5.重新签名，生成新的不带assets/Android资源的APK

    return

def Main():

    info = "\n请确认配置好以下环境变量：\n1.重签名：jarsigner（在JDK安装目录bin下）\n2.Obb生成：jobb（在Android SDK目录的tools下）\n3.查看签名：aapt（在Android SDK目录build-tools中任意一个版本目录下）\n"
    print info;

    while True:
        s = raw_input("\n请选择操作类型：0.根据整APK生成拆分APK  1.自动签名  2.生成obb  3.退出\n")
        if (s.isdigit()):
            cmdId = int(s)
            if (cmdId in [0,1, 2, 3]):
                if (cmdId == 3):
                    break;
            if (cmdId == 1):
                AutoSign();
            elif (cmdId == 2):
                BuildObb();
            elif (cmdId == 0):
                buildFromApk();
    return;

##################################### 调用入口 ###################################
if __name__ == '__main__':
    Main()
#################################################################################
