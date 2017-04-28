/* Cydia Substrate - Powerful Code Insertion Platform
 * Copyright (C) 2008-2011  Jay Freeman (saurik)
*/

/* GNU Lesser General Public License, Version 3 {{{ */
/*
 * Substrate is free software: you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version.
 *
 * Substrate is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with Substrate.  If not, see <http://www.gnu.org/licenses/>.
**/
/* }}} */

#ifndef SUBSTRATE_LOG_HPP
#define SUBSTRATE_LOG_HPP

#if 0

#include <CoreFoundation/CFLogUtilities.h>

#define MSLog(level, format, ...) CFLog(level, CFSTR(format), ## __VA_ARGS__)

#define MSLogLevelNotice kCFLogLevelNotice
#define MSLogLevelWarning kCFLogLevelWarning
#define MSLogLevelError kCFLogLevelError

#else

#include <syslog.h>

#if __COREFOUNDATION__

#define MSLog(level, format, ...) do { \
    CFStringRef _formatted(CFStringCreateWithFormat(kCFAllocatorDefault, NULL, CFSTR(format), ## __VA_ARGS__)); \
    size_t _size(CFStringGetMaximumSizeForEncoding(CFStringGetLength(_formatted), kCFStringEncodingUTF8)); \
    char _utf8[_size + sizeof('\0')]; \
    CFStringGetCString(_formatted, _utf8, sizeof(_utf8), kCFStringEncodingUTF8); \
    CFRelease(_formatted); \
    syslog(level, "%s", _utf8); \
} while (false)

#else

#define MSLog(level, format, ...) do { \
    syslog(level, format, ## __VA_ARGS__); \
} while (false)

#endif

#define MSLogLevelNotice LOG_NOTICE
#define MSLogLevelWarning LOG_WARNING
#define MSLogLevelError LOG_ERR

#endif

#endif//SUBSTRATE_LOG_HPP
