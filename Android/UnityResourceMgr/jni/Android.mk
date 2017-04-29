LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)
LOCAL_MODULE := libmain
LOCAL_SRC_FILES := libmain.so

include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := libunity
LOCAL_SRC_FILES := libunity.so

include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)
LOCAL_MODULE := libmono
LOCAL_SRC_FILES := libmono.so

include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)

LOCAL_MODULE    := gdmo

LOCAL_SHARED_LIBRARIES := mono
	
LOCAL_LDLIBS := \
	-llog \
	-ldl
	
LOCAL_SRC_FILES := PosixMemory.cpp \
		hde64.c \
		StrConvert.cpp \
		Hooker.cpp \
		Debug.cpp \
		myhook.cpp \
		init.c
		
	
LOCAL_CPPFLAGS+=-fexceptions
include $(BUILD_SHARED_LIBRARY)






