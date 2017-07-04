#!/usr/bin/python #coding=utf-8

'''
自动化打包脚本
'''

import os, sys, platform
import  configfile
import  tail
import  subprocess
import re

#######全局变量

#输入的APK版本号
global ApkVersion
ApkVersion = "2.0.0.0"
#基础资源版本号
global BaseResVersion
BaseResVersion = "1.0.0.0"
#增量资源版本号
global AppendResVersion
AppendResVersion = "1.0.0.0"
#输入平台：0-Windows 1-Android 2-IOS
global BuildPlatform
BuildPlatform = -1
#IOS AppName
global IOSAppName
IOSAppName = "IosBuild"
#Win AppName
global WinAppName
WinAppName = "client.exe"
##############

def CheckVersionFormat(version, startVer):
    if version == None:
        return False
    version = version.strip()
    if version == "":
        return False
    #起始位置匹配
    fmt = "%s.\d+.\d+.\d+" % startVer
    ver = re.match(fmt, version)
    if ver == None:
        return False, None
    return True, ver.group()

def UserInputVersion():

    global BuildPlatform
    global BaseResVersion
    global AppendResVersion


    while True:

        while True:
            s = raw_input("请输入打包平台(0-Windows 1-Android 2-IOS)：")
            if (s.isdigit()):
                BuildPlatform = int(s)
                if BuildPlatform in [0, 1, 2]:
                    if (IsWindowsPlatform() and (BuildPlatform == 2)):
                        print "Windows平台无法打包IOS"
                    else:
                        break
                else:
                    print "\n无此平台打包\n"

        '''
        while True:
            ApkVersion = raw_input("请输入APK版本号(格式：2.x.x.x)：")
            isFmtOk, ApkVersion = CheckVersionFormat(ApkVersion, "2")
            if isFmtOk:
                break
            else:
                print "\n版本号格式错误\n"
        '''
        while True:
            BaseResVersion = raw_input("请输入基础资源版本号(格式：1.x.x.x)：")
            isFmtOk, BaseResVersion = CheckVersionFormat(BaseResVersion, "1")
            if isFmtOk:
                break
            else:
                print "\n版本号格式错误\n"
        '''
        while True:
            AppendResVersion = raw_input("请输入增量资源版本号(格式：1.x.x.x)：")
            isFmtOk, AppendResVersion = CheckVersionFormat(AppendResVersion, "1")
            if isFmtOk:
                break
            else:
                print "\n版本号格式错误\n"
        '''
        isYes = False
        checkStr = "\n目标平台: %s   基础资源版本: %s    是否确认开始打包？(y/n)\n"
        if BuildPlatform == 0:
            checkStr = checkStr % ("Windows", BaseResVersion)
        elif BuildPlatform == 1:
            checkStr = checkStr % ("Android", BaseResVersion)
        elif BuildPlatform == 2:
            checkStr = checkStr % ("IOS", BaseResVersion)

        while True:
            s = raw_input(checkStr)
            if s == 'y' or s == 'Y':
                isYes = True
                break
            elif s == 'n' or s == 'N':
                isYes = False
                break
            else:
                break
        if isYes:
            break
        else:
            print ""

    return

def SaveVersionInfo():
    global  BaseResVersion

    fileName = "%s/buildVersion.cfg" % GetUnityOrgProjPath()
    file = open(fileName, "w")
    if file == None:
        return
    file.write(BaseResVersion)
    return

def LoadVersionInfo():

    global BaseResVersion
    global AppendResVersion

    fileName = "%s/buildVersion.cfg" % GetUnityOrgProjPath()
    if (not os.path.exists(fileName)) or (not os.path.isfile(fileName)):
        return
    file = open(fileName, "r");
    if file == None:
        return
    version = file.readline()

    if version.strip() == "":
        version = "1.0.0.0"

    BaseResVersion = version
    AppendResVersion = version

    print "当前资源版本: %s \n" % version
    return

def GetUnityOrgProjPath():
    result = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
    return result

def GetUnityProjPath():
    result = GetUnityOrgProjPath() + "/outPath/Proj"
    return result

def GetUnityOrgOutPath():
    outPath = GetUnityOrgProjPath() + "/outPath"
    return outPath

def IsWindowsPlatform():
    return "Windows" in platform.system()

def IsMacPlatform():
    return "Darwin" in platform.system()

def MonitorLine(txt):
    print txt

def UnityBuildABProj():

    global BuildPlatform

    outPath = GetUnityOrgOutPath()

    if (not os.path.exists(outPath)) or (not os.path.isdir(outPath)):
        os.mkdir(outPath)


    logFile = "%s/autobuild.txt" % outPath;
    f = open(logFile, "w")
    f.close()

    montior = tail.Tail(logFile)
    montior.register_callback(MonitorLine)

    projPath = GetUnityProjPath()
    if (not os.path.exists(projPath)) or (not os.path.isdir(projPath)):
        if IsMacPlatform():
            cmd = "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
            if not os.path.exists(cmd):
                print "\n未安装Unity, 请下载Unity!!!"
                return False
            print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在创建工程...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
            cmd = "%s -quit -batchmode -nographics -createProject %s -logFile %s" % (cmd, projPath, logFile)
            process = subprocess.Popen(cmd, shell=True)
            montior.follow(process, 2)
        elif IsWindowsPlatform():
            print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在创建工程...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
            cmd = "Unity.exe -quit -batchmode -nographics -createProject %s -logFile %s" % (projPath, logFile)
            process = subprocess.Popen(cmd, shell=True)
            montior.follow(process, 2)
        else:
            print "不支持此平台打包"
            return False


    cmd = ""
    if IsMacPlatform():
        cmd = "/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -nographics -projectPath %s -executeMethod %s -logFile " + logFile
    elif IsWindowsPlatform():
        cmd = "Unity.exe -quit -batchmode -nographics -projectPath %s -executeMethod %s -logFile " + logFile
    else:
        print "不支持此平台打包"
        return False

    if not BuildPlatform in [0, 1, 2]:
        return False

    copyCmd = cmd % (GetUnityOrgProjPath(), "AssetBundleBuild.Cmd_Build_Copy")
    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在拷贝文件...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"

    process = subprocess.Popen(copyCmd, shell=True)
    montior.follow(process, 2)

    func = ""
    if BuildPlatform == 1:
        func = "AssetBundleBuild.Cmd_Build_Android_ABLz4_Append"
    elif BuildPlatform == 2:
        func = "AssetBundleBuild.Cmd_Build_IOS_ABLz4_Append"
    elif BuildPlatform == 0:
        func = "AssetBundleBuild.Cmd_BuildWin32_Lz4"
    else:
        return False

    cmd = cmd % (GetUnityOrgProjPath(), func)
   # print cmd
    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>开始生成AssetBundle...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"

    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)

    return True

#导出ANDROID APK
def UnityAndroidProjToApk():

    logFile = "%s/autobuild.txt" % GetUnityOrgOutPath();
    f = open(logFile, "w")
    f.close()

    projPath = GetUnityProjPath()

    #生成APK
    cmd = ""
    if IsMacPlatform():
        cmd = "/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -nographics -projectPath %s -executeMethod %s -logFile " + logFile
    elif IsWindowsPlatform():
        cmd = "Unity.exe -quit -batchmode -nographics -projectPath %s -executeMethod %s -logFile " + logFile
    else:
        print "不支持此平台打包"
        return False

    montior = tail.Tail(logFile)
    montior.register_callback(MonitorLine)

    cmd = cmd % (projPath, "AssetBundleBuild.Cmd_Apk")
    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在生成APK...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"

    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)

    return True

#导出WINDOWS的EXE
def UnityWinProjToExe():

    global WinAppName

    logFile = "%s/autobuild.txt" % GetUnityOrgOutPath();
    f = open(logFile, "w")
    f.close()

    projPath = GetUnityProjPath()

    #生成EXE
    cmd = ""
    if IsMacPlatform():
        cmd = "/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -nographics -projectPath %s -executeMethod %s -logFile " + logFile
    elif IsWindowsPlatform():
        cmd = "Unity.exe -quit -batchmode -nographics -projectPath %s -executeMethod %s -logFile " + logFile
    else:
        print "不支持此平台打包"
        return False

    montior = tail.Tail(logFile)
    montior.register_callback(MonitorLine)

    cmd = cmd % (projPath, "AssetBundleBuild.Cmd_Win")
    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在生成EXE...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"

    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)

    winExe = "%s/%s" % (GetWinExportProjPath(), WinAppName)
    if (not os.path.exists(winExe)) or (not os.path.isfile(winExe)):
        print "\n生成EXE失败~~!!!\n"
        return False

    return True

#导出的IOS_BUID工程路径
def GetIOSExportProjPath():
    ret = GetUnityOrgProjPath() + "/outPath/IOS_Build"
    return ret

def GetWinExportProjPath():
    ret = GetUnityOrgProjPath() + "/outPath/Win_Build"
    return ret;

def UnityIOSProjToIPA():

    global  IOSAppName

    logFile = "%s/autobuild.txt" % GetUnityOrgOutPath()
    f = open(logFile, "w")
    f.close()

    projPath = GetUnityProjPath()

    #生成XCode工程
    cmd = ""
    if IsMacPlatform():
        cmd = "/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -nographics -projectPath %s -executeMethod %s -logFile " + logFile
    else:
        print "不支持此平台打包"
        return False

    montior = tail.Tail(logFile)
    montior.register_callback(MonitorLine)

    cmd = cmd % (projPath, "AssetBundleBuild.Cmd_IOS")
    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在生成XCode工程...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"

    process = subprocess.Popen(cmd, shell=True)
    montior.follow(process, 2)

    iosExportPath = GetIOSExportProjPath()
    archiveFileName = "%s/%s.xcarchive" % (iosExportPath, IOSAppName)
    defaultIPAFileName = "%s/Unity-iPhone.ipa" % iosExportPath

    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在清理XCode工程...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
    # 先清理工程
    #os.system("xcodebuild clean -project %s/Unity-iPhone.xcodeproj -target Unity-iPhone -configuration Release -sdk iphoneos" % iosExportPath)
    os.system("xcodebuild clean -project %s/Unity-iPhone.xcodeproj -target Unity-iPhone" % iosExportPath)
    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在生成archive...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
    os.system("xcodebuild archive -project %s/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release -sdk iphoneos -archivePath %s" %
              (iosExportPath, archiveFileName))

    #判断文件是否存在
    if (not os.path.exists(archiveFileName)) or (not os.path.isfile(archiveFileName)):
        print "\n生成archive失败~~!!!\n"
        return False

    print ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>正在生成IPA...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"

    print "======>>生成ad-hoc Ipa:"
    os.system("xcodebuild -exportArchive -archivePath %s -exportOptionsPlist %s/build/ad-hoc.plist -exportPath %s" %
              (archiveFileName, iosExportPath, iosExportPath))
    if (not os.path.exists(defaultIPAFileName)) or (not os.path.isfile(defaultIPAFileName)):
        print "\n生成ad-hoc ipa失败~~!!!\n"
        return False

    #生成ad-hoc
    # IPA改名
    os.rename(defaultIPAFileName, "%s/%s_ad-hoc.ipa" % (iosExportPath, IOSAppName))

    #生成AppStore
    print "====>>生成AppStore Ipa:"
    os.system("xcodebuild -exportArchive -archivePath %s -exportOptionsPlist %s/build/appstore.plist -exportPath %s" %
              (archiveFileName, iosExportPath, iosExportPath))
    if (not os.path.exists(defaultIPAFileName)) or (not os.path.isfile(defaultIPAFileName)):
        print "\n生成appstore ipa失败~~!!!\n"
        return False

    # IPA改名
    os.rename(defaultIPAFileName, "%s/%s.ipa" % (iosExportPath, IOSAppName))

    return True

#原始工程直接导入WIN工程
def UnityToExe():
    projPath = GetUnityOrgProjPath()
    if (not os.path.exists(projPath)) or (not os.path.isdir(projPath)):
        print "项目为空"
        return False
    if IsMacPlatform():
        cmd = "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
        if not os.path.exists(cmd):
            print "\n未安装Unity, 请下载Unity!!!"
            return False
        os.system("%s -quit -batchmode -nographics -projectPath %s -executeMethod AssetBundleBuild.Cmd_Win" % (cmd, projPath))
    elif IsWindowsPlatform():
        os.system("Unity.exe -quit -batchmode -nographics -projectPath %s -executeMethod AssetBundleBuild.Cmd_Win" % projPath)
    return True

# 主函数
def Main():

    global BuildPlatform

    if (IsWindowsPlatform()):
        print "打包前请确认设置好Unity.exe环境变量"

    LoadVersionInfo()
    UserInputVersion()
    SaveVersionInfo()

    '''
    # Windows平台直接生成EXE
    if BuildPlatform == 0:
        UnityToExe()
    else:
        UnityBuildABProj()
        if BuildPlatform == 1:
            UnityAndroidProjToApk()
        elif BuildPlatform == 2:
            UnityIOSProjToIPA()
        else:
            print "不支持此平台"
    '''

    UnityBuildABProj()
    if BuildPlatform == 1:
        UnityAndroidProjToApk()
    elif BuildPlatform == 2:
        UnityIOSProjToIPA()
    elif BuildPlatform == 0:
        UnityWinProjToExe()
    else:
        print "不支持此平台"


    print "\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>所有执行完毕...>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n"
    return

##################################### 调用入口 ###################################
if __name__ == '__main__':
    Main()
#################################################################################
