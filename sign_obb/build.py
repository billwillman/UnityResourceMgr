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
import  tail
import xml.etree.ElementTree as ET
import xml.dom.minidom as minidom
import hashlib

def AutoSignFrom(unsignPath, signPath):
    keystorePath = "";
    while True:
        s = raw_input("\n请输入.keystore的文件路径：\n")
        if (s != None):
            s.strip();
        if (s == None or len(s) <= 0):
            continue;
        keystorePath = s;
        break;

    keystoreAlias = "";
    while True:
        s = raw_input("\n请输入keystore的alias：\n")
        if (s != None):
            s.strip();
        if (s == None or len(s) <= 0):
            continue;
        keystoreAlias = s;
        break;

    keystorePassword = "";
    while True:
        s = raw_input("\n请输入keystore的密码：\n")
        if (s != None):
            s.strip();
        if (s == None or len(s) <= 0):
            continue;
        keystorePassword = s;
        break;

    cmd = "jarsigner -verbose -keystore %s -storepass %s -signedjar %s %s %s" % \
          (keystorePath, keystorePassword, signPath, unsignPath, keystoreAlias);

    print cmd;

    os.system(cmd);

    return

def AutoSign():
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

    AutoSignFrom(unsignPath, signPath);

    return

# 使用ZIP来做OBB
def BuildObbZipFrom(fileDir, outDir, apkName, patchName, apkVersion):
    obbFileName = "%s.%d.%s.obb" % (patchName, apkVersion, apkName);
    outFileName = outDir + "/" + obbFileName;
    f = zipfile.ZipFile(outFileName, 'w', zipfile.ZIP_STORED);

    print "开始obb压缩..."
    idx =  fileDir.index("assets");
    startDir = fileDir;
    if (idx >= 0):
        startDir = fileDir[0:idx];
    for path, dirnames, filenames in os.walk(fileDir):
        # 去掉目标跟路径，只对目标文件夹下边的文件及文件夹进行压缩
        fpath = path.replace(startDir, '')
        for filename in filenames:
            s = "obb压缩=》%s" % filename;
            print s;
            f.write(os.path.join(path, filename), os.path.join(fpath, filename))

    f.close();
    print "obb压缩完成..."
    return outFileName;

def BuildObbFrom(fileDir, outDir, apkName, patchName, apkVersion):
    logFile = os.path.dirname(os.path.realpath(__file__)) + "/jobb.txt";

    # 如果文件不存在则重新生成
    f = open(logFile, "w")
    f.close()

    montior = tail.Tail(logFile)
    montior.register_callback(MonitorLine)

    obbFileName = "%s.%d.%s.obb" % (patchName, apkVersion, apkName);

    outFileName = outDir + "/" + obbFileName;

    #cmd = "java -jar jobb.jar -d %s -o %s -pn %s -pv %d" % (fileDir, outFileName, apkName, apkVersion);
    cmd = "jobb -d %s -o %s -pn %s -pv %d -v %s -ov" % (fileDir, outFileName, apkName, apkVersion, logFile);
    print cmd;

    #os.system(cmd);
    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)

    while True:
        s = raw_input("\n是否提取生成的obb检查？(Y/N)\n")
        if (s != None):
            s.strip();
        if s == 'y' or s == 'Y':
            dumpDir = "%s.%d.%s.dump" % (patchName, apkVersion, apkName);
            dumpDir = outDir + "/" + dumpDir;
            if (os.path.exists(dumpDir) and os.path.isdir(dumpDir)):
                print "正在删除目录：%s" % dumpDir;
                shutil.rmtree(dumpDir)
            cmd = "jobb -dump %s -d %s -v %s " % (outFileName, dumpDir, logFile);
            print cmd;
            process = subprocess.Popen(cmd, shell=True)
            montior.follow(process, 2)
            break
        elif s == 'n' or s == 'N':
            break;

    return outFileName;

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

    BuildObbFrom(fileDir, outDir, apkName, patchName, apkVersion);
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

def MonitorLine(txt):
    print txt

#较大文件处理
def md5sum(file_path):
  f = open(file_path,'rb')
  md5_obj = hashlib.md5()
  while True:
    d = f.read(8096)
    if not d:
      break
    md5_obj.update(d)
  hash_code = md5_obj.hexdigest()
  f.close()
  md5 = str(hash_code).lower()
  return md5

#修改settings.xml的设置
def writeObbSettings(settingfileName, obbFileName):
    print "开始写入 %s" % settingfileName;
    tree = ET.parse(settingfileName);
    root = tree.getroot();
    for child in root:
        name = child.get('name');
        if (name == None):
            continue;
        if (name.lower() != "useobb"):
            continue;
        child.text = "True";
        break;

    # 增加MD5选项
    fmd5 = md5sum(obbFileName);
    newNode = ET.SubElement(root, "bool", {"name": fmd5});
    newNode.text = "True";

    print "obb %s md5=> %s" % (obbFileName, fmd5);

    tree.write(settingfileName, encoding="utf-8",xml_declaration=True);

    print "写入Settings.xml完毕..."
    return;

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

    # 如果文件不存在则重新生成
    f = open(logFile, "w")
    f.close()

    montior = tail.Tail(logFile)
    montior.register_callback(MonitorLine)

    cmd = "aapt dump badging %s > %s" % (srcApkFile, logFile);
    # os.system(cmd);
    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)

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

    idx = srcApkFile.rfind('.');
    if (idx < 0):
        return;

    unzipDir = srcApkFile[0:idx];
    # 1.如果原来目录有文件删除掉所有文件
    if (os.path.exists(unzipDir) and os.path.isdir(unzipDir)):
        print "正在删除目录：%s" % unzipDir;
        shutil.rmtree(unzipDir)

    #2.解压APK
    print "%s 开始解压APK，解压目录： %s" % (srcApkFile, unzipDir)
    f = zipfile.ZipFile(srcApkFile, 'r')
    for file in f.namelist():
        s = file;
        s = s.decode("ascii").encode("utf-8")
        s = "解压=>%s" % s;
        print s;
        f.extract(file, unzipDir)
    f.close();
    print "解压APK完成..."
    #3.assets/Android目录打OBB
    #obb目录
    obboutPath = os.path.dirname(srcApkFile);
    obbSrcPath = "%s/assets/Android" % unzipDir;
    s = "%s 生成obb" % obbSrcPath;
    print s;

    #选择JOBB还是其他
    obbFileName = "";
    while True:
        s = raw_input("\n选择生成OBB方式：1.Zip生成 2.JObb生成\n");
        if (s.isdigit()):
            cmdId = int(s)
            if cmdId in [1, 2]:
                if cmdId == 1:
                    obbFileName = BuildObbZipFrom(obbSrcPath, obboutPath, packageName, "main", versionCode);
                    break;
                elif cmdId == 2:
                    obbFileName = BuildObbFrom(obbSrcPath, obboutPath,packageName, "main", versionCode);
                    break;
    # 重新写入settings.xml
    settingsFileName = "%s/assets/bin/Data/settings.xml" % unzipDir;
    writeObbSettings(settingsFileName, obbFileName);

    #4.删除assets/Android目录
    s = "%s 删除" % obbSrcPath;
    print s;
    shutil.rmtree(obbSrcPath);
    metaDir = "%s/META-INF" % unzipDir;
    s = "%s 删除" % metaDir;
    print s;
    shutil.rmtree(metaDir);
    #5.压缩解压删除后的，并变成JAR
    zipFileName = unzipDir + ".jar";
    s = "重新压缩 %s" % zipFileName
    f = zipfile.ZipFile(zipFileName, 'w', zipfile.ZIP_DEFLATED);

    #for dirpath, dirnames, filenames in os.walk(unzipDir):
     #   for file in filenames:
      #      f.write(file, zipFileName)
    for path, dirnames, filenames in os.walk(unzipDir):
        # 去掉目标跟路径，只对目标文件夹下边的文件及文件夹进行压缩
        fpath = path.replace(unzipDir, '')
        for filename in filenames:
            s = "压缩=》%s" % filename;
            print s;
            f.write(os.path.join(path, filename), os.path.join(fpath, filename))

    f.close();
    print "重新压缩完成"
    #6.重新签名，生成新的不带assets/Android资源的APK
    print "开始重签名..."
    signApkFileName = zipFileName[0:len(zipFileName) - 4] + "_sign.apk";
    AutoSignFrom(zipFileName, signApkFileName);
    print "生成签名APK完成"

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
