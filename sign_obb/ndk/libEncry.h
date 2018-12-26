#pragma once
#include <jni.h>

#ifdef __cplusplus
extern "C" {
#endif

	// 后面通过字符串提供掉com_package_demo
	// java层NsEncry类中的CheckSign方法
	JNIEXPORT void JNICALL Java_com_package_demo_NsEncry_CheckSign(JNIEnv*);

#ifdef __cplusplus
}
#endif