LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)
#LOCAL_MODULE表示生成的动态库名为NsEncry
LOCAL_MODULE    := NsEncry
LOCAL_C_INCLUDE := libEncry.h \
#LOCAL_SRC_FILES表示使用到的类
#LOCAL_SRC_FILES := libEncry.cpp \

LOCAL_LDLIBS    := -lm -llog -ljnigraphics

include $(BUILD_SHARED_LIBRARY)