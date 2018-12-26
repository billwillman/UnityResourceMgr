#pragma once
#include <jni.h>

#ifdef __cplusplus
extern "C" {
#endif

	// java层NsEncry类中的CheckSign方法
	JNIEXPORT void JNICALL Java_com_package_NsEncry_NsEncry_CheckSign(JNIEnv*);

#ifdef __cplusplus
}
#endif