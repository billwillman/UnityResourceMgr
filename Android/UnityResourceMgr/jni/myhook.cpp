
#include <stdio.h>
#include <unistd.h>
#include <dlfcn.h>
#include <android/log.h>
#include "CydiaSubstrate.h"
#include "myhook.h"
//#include <jni.h>
//#include <dlfcn.h>
#define  LOGW(...)  __android_log_print(ANDROID_LOG_WARN,"sdgsec",__VA_ARGS__)



extern "C" void* mono_image_open_from_data_with_name(char *, unsigned int , int , void *, int , const char *);

void* (*old_mono_image_open_from_data_with_name)(char *, unsigned int , int , void *, int , const char *);

// 可写路径
static char* m_WritePath = NULL;

char* jstringTostring(JNIEnv* env, jstring jstr)
{
	char* rtn = NULL;
	jclass clsstring = env->FindClass("java/lang/String");
	jstring strencode = env->NewStringUTF("utf-8");
	jmethodID mid = env->GetMethodID(clsstring, "getBytes", "(Ljava/lang/String;)[B");
	jbyteArray barr= (jbyteArray)env->CallObjectMethod(jstr, mid, strencode);
	jsize alen = env->GetArrayLength(barr);
	jbyte* ba = env->GetByteArrayElements(barr, JNI_FALSE);
	if (alen > 0)
	{
		rtn = (char*)malloc(alen + 1);
		memcpy(rtn, ba, alen);
		rtn[alen] = 0;
	}
	env->ReleaseByteArrayElements(barr, ba, 0);
	return rtn;
}

// 初始化可写目录
void Java_com_UnityResources_Test_SendWritePath(JNIEnv* env, jstring path)
{
	if (!m_WritePath)
	{
		m_WritePath = jstringTostring(env, path);
	}
}

void *
my_mono_image_open_from_data_with_name (char *data, unsigned int data_len, int need_copy, void *status, int refonly, const char *name)
{
	//MonoCLIImageInfo *iinfo;
	//MonoImage *image;
	//char *datac;
//在这里插入libhack.so/hack.cpp中的HackMonoDll函数，暂时只有一句打印没有其他内容，等hook完成之后，我们再自行修改so进行功能扩展
	//HackMonoDll(&data,&data_len,name);

//执行HackMonoDll后下面继续执行原来函数

	// 找到路径
	InitWritePath();

	return old_mono_image_open_from_data_with_name(data, data_len, need_copy, status, refonly, name);
}







int HookMonoFuc()
{


	MSHookFunction((void*)&mono_image_open_from_data_with_name,
			        			(void*)&my_mono_image_open_from_data_with_name,
			        			(void **)&old_mono_image_open_from_data_with_name);

}
