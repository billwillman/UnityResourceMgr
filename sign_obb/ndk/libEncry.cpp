#include "libEncry.h"
#include <android/log.h>

// LOG宏定义
#define LOGD(...) __android_log_print(ANDROID_LOG_DEBUG , "Unity", __VA_ARGS__)
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO , "Unity", __VA_ARGS__)
#define LOGW(...) __android_log_print(ANDROID_LOG_WARN , "Unity", __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR , "Unity", __VA_ARGS__)

// 后面通过字符串提供掉com_package_demo
JNIEXPORT void JNICALL Java_com_package_demo_NsEncry_CheckSign(JNIEnv* env)
{

}