
#include <stdio.h>

#include <vector>
#include <string>
#include "StrConvert.h"

#include <unistd.h>
#include <dlfcn.h>
#include <android/log.h>
#include "CydiaSubstrate.h"
#include "myhook.h"
//#include <jni.h>
//#include <dlfcn.h>

#define  LOG_TAG    "Unity"
#define  LOGI(...)  __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#define LOGE(...)  __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)
#define LOGI(...)  __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)



extern "C" void* mono_image_open_from_data_with_name(char *, unsigned int , int , void *, int , const char *);

void* (*old_mono_image_open_from_data_with_name)(char *, unsigned int , int , void *, int , const char *);

// 可写路径
static std::string m_ExternPath = std::string();
static std::string m_InternPath = std::string();
static bool m_InitWritePath = false;

using namespace StrConv;

std::string getStringUTFCharsJNI(JNIEnv* env, jstring srcjStr)
{
	std::string ret;
	if(srcjStr != NULL)
	{
		 const UTF16* unicodeChar = ( const UTF16*)env->GetStringChars(srcjStr, JNI_FALSE);
		 if (unicodeChar)
		 {
			 size_t unicodeCharLength = env->GetStringLength(srcjStr);
			 // 路径不要大于1023
			 if (unicodeCharLength < 1024)
			 {
				 UTF8 buf[1024] = {0};
				 UTF16ToUTF8(unicodeChar, unicodeChar + unicodeCharLength + 1, buf, buf + unicodeCharLength + 1);
				 ret = (char*)buf;
			 }

			 env->ReleaseStringChars(srcjStr, unicodeChar);
		 } else
			 ret = "";
	} else
		ret = "";
	return ret;
}
/*
 * If your app does not have WRITE_EXTERNAL_STORAGE permission, this will always return the internal data path.
If your app does have WRITE_EXTERNAL_STORAGE permission, and an SD card is available, it will return the external data path.
If your app does have WRITE_EXTERNAL_STORAGE permission, and no SD card is available, it will return the internal data path.
 * */
// 初始化可写目录
void Java_com_UnityResources_Test_UnityResourceMain_SendWritePath(JNIEnv* env, jobject obj,
		jstring internalPath,
		jstring externPath)
{
	if (!m_InitWritePath)
	{
		m_InitWritePath = true;
		m_InternPath = getStringUTFCharsJNI(env, internalPath);
		m_ExternPath = getStringUTFCharsJNI(env, externPath);
	}
}

char *ReadDllData(const char *pathName, size_t *size)
{
    FILE *file = fopen (pathName, "rb");
    if (file == NULL)
    {
        return NULL;
    }
    fseek (file, 0, SEEK_END);
    int length = ftell(file);
    fseek (file, 0, SEEK_SET);
    if (length < 0)
    {
        fclose (file);
        return NULL;
    }
    *size = (size_t)length;
    char *outData = (char*)malloc(length);
    if (!outData)
    	return NULL;
    int readLength = fread (outData, 1, length, file);
    fclose(file);
    if (readLength != length)
    {
        free (outData);
        return NULL;
    }
    return outData;
}

bool processDllData(char*& data, unsigned int& data_len, const char* name, const std::string& externPath)
{
	if (m_InitWritePath && !externPath.empty() && name && data)
		{
			std::string path = name;
			size_t splitPos = path.find_last_of('/');
			std::string fileName;
			if (splitPos == std::string::npos)
			{
				fileName = path;
			} else
			{
				fileName = path.substr(splitPos + 1);
			}
			if (!fileName.empty())
			{
				char newPath[1024] = {0};
				if (fileName[fileName.size() - 1] != '/')
					sprintf(newPath, "%s/%s", externPath.c_str(), fileName.c_str());
				else
					sprintf(newPath, "%s%s", externPath.c_str(), fileName.c_str());

				size_t newDataSize;
				char* newData = ReadDllData(newPath, &newDataSize);
				if (newData)
				{
					LOGE("oldpath=%s newPath=%s", path.c_str(), newPath);

				//	return true;

					free(data);
					data = newData;
					data_len = newDataSize;


					return true;
				} else
					LOGE("[not] found=>oldpath=%s newPath=%s", path.c_str(), newPath);

			}
		}

	return false;
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
	bool isExternaDll = false;
	if (!processDllData(data, data_len, name, m_ExternPath))
	{
		if (processDllData(data, data_len, name, m_InternPath))
			isExternaDll = true;
	} else
		isExternaDll = true;

	void * ret = old_mono_image_open_from_data_with_name(data, data_len, need_copy, status, refonly, name);
	if (isExternaDll)
		free(data);
	return ret;
}







int HookMonoFuc()
{


	MSHookFunction((void*)&mono_image_open_from_data_with_name,
			        			(void*)&my_mono_image_open_from_data_with_name,
			        			(void **)&old_mono_image_open_from_data_with_name);

}
