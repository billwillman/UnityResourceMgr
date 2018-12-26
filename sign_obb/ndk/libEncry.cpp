#include "libEncry.h"
#include <android/log.h>

// LOG∫Í∂®“Â
#define LOGD(...) __android_log_print(ANDROID_LOG_DEBUG , "Unity", __VA_ARGS__)
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO , "Unity", __VA_ARGS__)
#define LOGW(...) __android_log_print(ANDROID_LOG_WARN , "Unity", __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR , "Unity", __VA_ARGS__)

JNIEXPORT void JNICALL Java_com_NsEncryPackage_NsEncry_NsEncry_CheckSign(JNIEnv* env)
{
	LOGD("abc");
}