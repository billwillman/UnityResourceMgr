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

def get_files(path, rule):
    all = []
    for fpathe,dirs,fs in os.walk(path):   # os.walk获取所有的目录
        for f in fs:
            filename = os.path.join(fpathe,f)
            if filename.endswith(rule):  # 判断是否是".sfx"结尾
                all.append(filename)
    return all

# 使用ZIP来做OBB
def BuildObbZipFrom(fileDir, outDir, apkName, patchName, apkVersion, mp4Root):
    obbFileName = "%s.%d.%s.obb" % (patchName, apkVersion, apkName);
    outFileName = outDir + "/" + obbFileName;
    f = zipfile.ZipFile(outFileName, 'w', zipfile.ZIP_STORED);

    print "开始obb压缩..."
    idx = -1;
    try:
        idx =  fileDir.index("assets");
    except:
        idx = -1;
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

    # 查找mp4文件也写入OBB里
    if (mp4Root != None):
        mp4files = get_files(mp4Root, ".mp4");
        if mp4files != None:
            for fileName in mp4files:
                idx = -1;
                try:
                    idx = fileName.index("assets");
                except:
                    idx = -1;
                if (idx < 0):
                    continue;
                dstFileName = fileName[idx:];
                s = "obb压缩=》%s" % filename;
                print s;
                f.write(fileName, dstFileName);
                s = "删除=》%s" % fileName;
                print s;
                os.remove(fileName);


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
    index = -1;
    try:
        index = readStr.index("package: name='");
    except:
        index = -1;
    if (index < 0):
        return None, None;
    index += tmplen;
    readStr = readStr[index:len(readStr) - index];
    index = -1;
    try:
        index = readStr.index("'");
    except:
        index = -1;
    if (index < 0):
        return None, None;
    packageName = readStr[0:index];
    if (packageName != None):
        packageName.strip();

    readStr = readStr[index + 1:len(readStr) - index];
    tmplen = len("versionCode='");
    index = -1;
    try:
        index = readStr.index("versionCode='");
    except:
        index = -1;
    if (index < 0):
        return packageName, None;
    index += tmplen;
    readStr = readStr[index:len(readStr) - index];
    index = -1;
    try:
        index = readStr.index("'");
    except:
        index = -1;
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

def md5sumF(f):
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

def UnityMd5EncryFromF(f):
    md5_obj = hashlib.md5()
    f.seek(0, 2);
    # 文件大小
    length = f.tell();
    offset = length - min(length, 65558);
    f.seek(offset, 0);
    while True:
        d = f.read(1024)
        if not d:
            break
        md5_obj.update(d)
    hash_code = md5_obj.hexdigest()
    f.close()
    md5 = str(hash_code).lower()
    return md5;

# Unity的MD5计算方式
def UnityMd5Encry(file_path):
    f = open(file_path, 'rb')
    md5_obj = hashlib.md5()
    f.seek(0, 2);
    #文件大小
    length = f.tell();
    offset = length - min(length, 65558);
    f.seek(offset, 0);
    while True:
        d = f.read(1024)
        if not d:
            break
        md5_obj.update(d)
    hash_code = md5_obj.hexdigest()
    f.close()
    md5 = str(hash_code).lower()
    return md5;

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

    # UNITY特定MD5算法
    fmd5 = UnityMd5Encry(obbFileName);
    newNode = ET.SubElement(root, "bool", {"name": fmd5});
    newNode.text = "True";

    print "obb %s md5=> %s" % (obbFileName, fmd5);

    tree.write(settingfileName, encoding="utf-8",xml_declaration=True);

    print "写入Settings.xml完毕..."
    return;

# 注入自己的SO库到里面
def ImportEncrySOInApk(apkRootPath):
    # 1.编译自己的SO库(至少包括x86 armv7)
    # 2.反编译class.dex
    # 3.混淆并重新生成新的class.dex
    return False;

# 加密等
def EncryApk(apkRootPath):
    if apkRootPath == None:
        return False;
    if (os.path.exists(apkRootPath) and os.path.isdir(apkRootPath)):
        # 注入SO到APK中
        ret = ImportEncrySOInApk(apkRootPath);
        if not ret:
            return ret;
        return True;
    return False;

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
    mp4Root = "%s/assets" % unzipDir;
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
                    obbFileName = BuildObbZipFrom(obbSrcPath, obboutPath, packageName, "main", versionCode, mp4Root);
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

# 真正比较APK差异的
def diffApk(oldApkFileName, newApkFileName):
    if (not os.path.exists(oldApkFileName)) or (not os.path.isfile(oldApkFileName)):
        return False;
    if (not os.path.exists(newApkFileName)) or (not os.path.isfile(newApkFileName)):
        return False;

    logFile = os.path.dirname(os.path.realpath(__file__)) + "/aapt.txt";
    # 如果文件不存在则重新生成
    f = open(logFile, "w")
    f.close()
    montior = tail.Tail(logFile)
    montior.register_callback(MonitorLine)
    # 1.获取SrcApk的版本号
    print "读取老APK包信息..."
    cmd = "aapt dump badging %s > %s" % (oldApkFileName, logFile);
    # os.system(cmd);
    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)
    oldPackageName, oldVersionCode = getApkInfoFromLog(logFile);
    # 2.获取dstApk的版本号
    print "读取新APK包信息..."
    cmd = "aapt dump badging %s > %s" % (newApkFileName, logFile);
    # os.system(cmd);
    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)
    newPackageName, newVersionCode = getApkInfoFromLog(logFile);
    if cmp(oldPackageName, newPackageName) != 0:
        print ">>>oldPackageName != newPackageName<<<"
        return False;
    # 3.确认生成DIFF的ZIP包文件名
    while True:
        s = "\n是否生成版本号 %d 到 %d的差异？(Y/N)\n" % (oldVersionCode, newVersionCode);
        s = raw_input(s)
        if (s != None):
            s.strip();
        if s == 'y' or s == 'Y':
            break
        elif s == 'n' or s == 'N':
            return False;
    outDiffDir = os.path.dirname(os.path.realpath(newApkFileName));
    diffPath = "%s/%d-%d" % (outDiffDir, oldVersionCode, newVersionCode);
    if (os.path.exists(diffPath) and os.path.isdir(diffPath)):
        print "删除目录=》%s" % diffPath;
        shutil.rmtree(diffPath)

    srcApk = zipfile.ZipFile(oldApkFileName, 'r')
    dstApk = zipfile.ZipFile(newApkFileName, 'r')

    # 开始APK内部比较，发现不一样，读取出来放到DIFF里
    srcNameList = srcApk.namelist();
    dstNameList = dstApk.namelist();
    for dstName in dstNameList:
        s = dstName;
        s = s.decode("ascii").encode("utf-8")
        idx =  -1;
        try:
            idx = srcNameList.index(dstName);
        except:
            idx = -1;
        if (idx < 0):
            print "src not find=》%s" % s;
            dstApk.extract(dstName, diffPath);
        else:
            srcName = srcNameList[idx];
            srcF = srcApk.open(srcName);
            srcMd5 = md5sumF(srcF);
            srcF.close();
            dstF = dstApk.open(dstName);
            dstMd5 = md5sumF(dstF);
            dstF.close();
            if (cmp(srcMd5, dstMd5) != 0):
                print "Diff=》%s" % s;
                print "Diff Md5=> %s = %s" % (srcMd5, dstMd5);
                dstApk.extract(dstName, diffPath);

    # 生成dstApk没有的但srcApk有的文件
    for srcName in srcNameList:
        idx = -1;
        try:
            idx = dstNameList.index(srcName);
        except:
            idx = -1;
        if idx < 0:
            s = srcName;
            s = s.decode("ascii").encode("utf-8")
            # 判断目录是否存在，如果不存在创建
            if ((not os.path.exists(diffPath)) or (not os.path.isdir(diffPath))):
                os.makedirs(diffPath);
            delFileName = "%s/%s__" % (diffPath, s);
            delDir = os.path.dirname(delFileName)
            if ((not os.path.exists(delDir)) or (not os.path.isdir(delDir))):
                os.makedirs(delDir);
            print "delete file=>%s" % delFileName;
            f = open(delFileName, "w");
            f.close();



    srcApk.close();
    dstApk.close();

    #检测diffPath里是否有文件，如果有，则压缩
    zipFile = None;
    zipFileName = "%s.zip" % diffPath;
    if (os.path.exists(diffPath) and os.path.isdir(diffPath)):
        for dirpath, dirnames, filenames in os.walk(diffPath):
            # 压缩吧
            for fileName in filenames:
                if (zipFile == None):
                    zipFile = zipfile.ZipFile(zipFileName, 'w', zipfile.ZIP_DEFLATED);
                fpath = dirpath.replace(diffPath, '')
                if len(fpath) > 0:
                    if (fpath[0] == '\\' or fpath[0] == '/'):
                        fpath = fpath[1:];
                srcFileName = os.path.join(dirpath, fileName);
                dstFileName = os.path.join(fpath, fileName);
                print "%s 压缩=》%s" % (srcFileName, dstFileName);
                zipFile.write(srcFileName, dstFileName);

    if zipFile != None:
        zipFile.close();

    print "Apk差异生成完毕..."
    return True;

# 生成两个APK差异
def buildDiffApk():
    oldApkFileNmae = "";
    while True:
        s = raw_input("\n请设置老版本APK文件路径：\n");
        if (not os.path.exists(s)) or (not os.path.isfile(s)):
            continue;
        isApk = os.path.splitext(s)[-1].lower() == ".apk";
        if (not isApk):
            continue;
        oldApkFileNmae = s;
        break;

    newApkFileName = "";
    while True:
        s = raw_input("\n请设置新版本APK文件路径：\n");
        if (not os.path.exists(s)) or (not os.path.isfile(s)):
            continue;
        isApk = os.path.splitext(s)[-1].lower() == ".apk";
        if (not isApk):
            continue;
        newApkFileName = s;
        break;
    ret = diffApk(oldApkFileNmae, newApkFileName);
    return ret;

def combineApk_patchFrom(apkFileName, patchFileName):
    if (not os.path.exists(apkFileName)) or (not os.path.isfile(apkFileName)):
        return False;
    if (not os.path.exists(patchFileName)) or (not os.path.isfile(patchFileName)):
        return False;
    apkFileName = os.path.realpath(apkFileName);
    patchFileName = os.path.realpath(patchFileName);

    # 原始APK目录字典
    srcF = zipfile.ZipFile(apkFileName, 'r')

    # 获得原始APK的目录结构
    print "读取原始APK目录结构..."
    srcApkDirMap = {};
    for info in srcF.infolist():
        dir = os.path.dirname(info.filename);
        dir = dir.decode("ascii").encode("utf-8")
        if not srcApkDirMap.has_key(dir):
            print "目录：%s 压缩类型：%d" % (dir, int(info.compress_type));
            srcApkDirMap[dir] = info.compress_type;

    print "开始合并生成APK..."

    srcP = zipfile.ZipFile(patchFileName, 'r');

    outDstDir = os.path.dirname(apkFileName);
    print outDstDir;

    idx = apkFileName.rfind(".apk");
    dstFileName = apkFileName[0:idx];
    idx = dstFileName.index(outDstDir);
    dstFileName = dstFileName[idx + len(outDstDir) + 1:];
    print dstFileName;

    idx = patchFileName.rfind(".zip");
    patchName = patchFileName[0:idx];
    outPatchDir = os.path.dirname(patchName);
    idx = patchName.index(outPatchDir);
    patchName = patchName[idx+len(outPatchDir) + 1:];
    print patchName;

    dstFileName = "%s/%s_%s.apk" % (outDstDir, dstFileName, patchName)
    s = "生成=》%s" % dstFileName;
    print s;
    dstF = zipfile.ZipFile(dstFileName, "w");

    # 遍历SrcApk把没有的fileName写入
    for info in srcF.infolist():
        findInfo = None;
        isDelete = False;
        for pInfo in srcP.infolist():
            if cmp(info.filename, pInfo.filename) == 0:
                findInfo = pInfo;
                break;
            delFileName = info.filename + "__";
            if cmp(delFileName, pInfo.filename):
                isDelete = True;
        # 如果是删除文件，则直接跳过
        if isDelete:
            continue;
        if (findInfo == None):
            s = info.filename;
            s = s.decode("ascii").encode("utf-8")
            srcDir = os.path.dirname(info.filename);
            srcDir = srcDir.decode("ascii").encode("utf-8")
            compressType = srcApkDirMap[srcDir];
            if compressType == None:
                compressType = zipfile.ZIP_DEFLATED;
            s = "源APK写入=》%s 压缩类型：%d" % (s, int(compressType));
            print s;
            f = srcF.open(info.filename);
            buf = f.read();
            dstF.writestr(info.filename, buf, compressType);
            f.close();
        '''
        else:
            s = findInfo.filename;
            s = s.decode("ascii").encode("utf-8")
            srcDir = os.path.dirname(findInfo.filename);
            compressType = srcApkDirMap[srcDir];
            if compressType == None:
                compressType = zipfile.ZIP_DEFLATED;
            s = "Patch写入=》%s 压缩类型：%d" % (s, int(compressType));
            print s;
            f = srcP.open(findInfo.filename);
            buf = f.read();
            dstF.writestr(findInfo.filename, buf, compressType);
            f.close();
        '''

    # 再循环Patch新增的
    for pInfo in srcP.infolist():
        s = pInfo.filename;
        s = s.decode("ascii").encode("utf-8")
        #如果以 __ 结尾的文件为需要删除的文件，所以在写入的时候忽略
        if s.endswith("__"):
            continue;
        srcDir = os.path.dirname(pInfo.filename);
        compressType = srcApkDirMap[srcDir];
        if compressType == None:
            compressType = zipfile.ZIP_DEFLATED;
        s = "Patch写入=》%s 压缩类型：%d" % (s, int(compressType));
        print s;
        f = srcP.open(pInfo.filename);
        buf = f.read();
        dstF.writestr(findInfo.filename, buf, compressType);
        f.close();


    dstF.close();
    srcP.close();
    srcF.close();

    print "生成新APK完毕..."

    return True;

def combineApk_patch():
    oldApkFileNmae = "";
    while True:
        s = raw_input("\n请设置老版本APK文件路径：\n");
        if (not os.path.exists(s)) or (not os.path.isfile(s)):
            continue;
        isApk = os.path.splitext(s)[-1].lower() == ".apk";
        if (not isApk):
            continue;
        oldApkFileNmae = s;
        break;

    newPatchFileName = "";
    while True:
        s = raw_input("\n请设置Patch文件路径(.zip)：\n");
        if (not os.path.exists(s)) or (not os.path.isfile(s)):
            continue;
        isApk = os.path.splitext(s)[-1].lower() == ".zip";
        if (not isApk):
            continue;
        newPatchFileName = s;
        break;

    ret = combineApk_patchFrom(oldApkFileNmae, newPatchFileName);
    return ret;

def Main():

    info = "\n请确认配置好以下环境变量：\n1.重签名：jarsigner（在JDK安装目录bin下）\n2.Obb生成：jobb（在Android SDK目录的tools下）\n" \
           "3.查看签名：aapt（在Android SDK目录build-tools中任意一个版本目录下）\n"
    print info;

    while True:
        s = raw_input("\n请选择操作类型：0.根据整APK生成拆分APK  1.自动签名  2.生成obb  3.Apk差异生成  4.Apk+patch生成新APK  5.退出\n")
        if (s.isdigit()):
            cmdId = int(s)
            if (cmdId in [0,1, 2, 3, 4, 5]):
                if (cmdId == 5):
                    break;
            if (cmdId == 1):
                AutoSign();
            elif (cmdId == 2):
                BuildObb();
            elif (cmdId == 0):
                buildFromApk();
            elif (cmdId == 3):
                buildDiffApk();
            elif (cmdId == 4):
                combineApk_patch();
    return;

##################################### 调用入口 ###################################
if __name__ == '__main__':
    Main()
#################################################################################
