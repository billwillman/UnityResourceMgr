#ifndef _MYHOOK_H__
#define _MYHOOK_H__

#include <jni.h>

#ifdef __cplusplus
extern "C" {
JNIEXPORT void JNICALL Java_com_UnityResources_Test_UnityResourceMain_SendWritePath(JNIEnv*, jobject, jstring, jstring);
#endif

void StartCheckCrc();

int HookMonoFuc();


#ifdef __cplusplus
}

#endif

#endif
