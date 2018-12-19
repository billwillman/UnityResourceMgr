@echo off
if "%PATH_BASE%" == "" set PATH_BASE=%PATH%
set PATH=%CD%;%PATH_BASE%;

goto start
rem jarsigner -verbose -keystore build-tools/android/test.keystore -signedjar %1 %1 test
rem unsignedProject
echo %1
rem signedProject
echo %2
rem key store path
echo %3
rem key alias name
echo %4
rem key store pass
echo %5
rem key alias pass
echo %6
echo "jarsigner -verbose -keystore %3 -signedjar %2 %1 %4"
:start

jarsigner -verbose -keystore %3 -storepass %5 -keypass %6 -signedjar %2 %1 %4
rem pause