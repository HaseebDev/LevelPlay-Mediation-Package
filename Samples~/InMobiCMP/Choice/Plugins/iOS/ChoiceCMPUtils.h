//
//  ChoiceCMPUtils.h
//  UnityFramework
//
//  Created by Abdul Basit on 27/06/22.
//

#ifndef ChoiceCMPUtils_h
#define ChoiceCMPUtils_h

#import <objc/runtime.h>
#import <Foundation/Foundation.h>
#import <InMobiCMP/InMobiCMP-Swift.h>
#import "UnityChoiceStyle.h"

// Converts C style string to NSString
#define GetStringParam(_x_) ((_x_) != NULL ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""])
#define GetNullableStringParam(_x_) ((_x_) != NULL ? [NSString stringWithUTF8String:_x_] : nil)

@interface ChoiceCMPUtils : NSObject
+(char*) cStringCopy: (NSString*) input;
+(NSMutableDictionary<NSString*, NSDictionary<NSString*, id>*>*) getDictionaryFromJson:(const char*) json;
+(NSDictionary *) dictionaryFromObject:(id)obj;
+(NSString *) stringFromDict: (NSDictionary*) dict;
+(ChoiceStyle *) mapUnityChoiceStyleToChoiceStyle: (UnityChoiceStyle *)unityChoiceStyle;
+(NSMutableDictionary *)changeKeyName:(NSString *)fromKey to:(NSString *)toKey inDictionary:(NSDictionary *)dictionary;
@end

#endif /* ChoiceCMPUtils_h */
