//
//  ChoiceCMPUtils.m
//  UnityFramework
//
//  Created by Abdul Basit on 22/09/23.
//

#import <Foundation/Foundation.h>
#import "ChoiceCMPUtils.h"
#import <InMobiCMP/InMobiCMP-Swift.h>
#import "UnityChoiceStyle.h"
#import "UnityChoiceColor.h"

@implementation ChoiceCMPUtils

// Converts an NSString into a const char* ready to be sent to Unity
+(char*) cStringCopy: (NSString*) input
{
    const char* string = [input UTF8String];
    return string ? strdup(string) : NULL;
}

+(NSMutableDictionary<NSString*, NSDictionary<NSString*, id>*>*) getDictionaryFromJson:(const char*) json
{
    NSString* jsonString = GetStringParam(json);
    if (jsonString.length == 0)
        return nil;
    NSMutableDictionary<NSString*, NSDictionary<NSString*, id>*>* dict =
        [NSJSONSerialization JSONObjectWithData:[jsonString dataUsingEncoding:NSUTF8StringEncoding]
                                        options:NSJSONReadingMutableContainers
                                          error:nil];
    return dict.count > 0 ? dict : nil;
}

+(NSDictionary *) dictionaryFromObject:(id)obj
{
    if (obj == nil)
        return nil;
    
    NSMutableDictionary *dict = [NSMutableDictionary dictionary];

    unsigned count;
    objc_property_t *properties = class_copyPropertyList([obj class], &count);

    for (int i = 0; i < count; i++) {
        NSString *key = [NSString stringWithUTF8String:property_getName(properties[i])];
        if([obj valueForKey:key] != nil) {
            if ([[obj valueForKey:key] isKindOfClass:[NSDictionary class]]
                && [self isAllKeysOfTypeNSNumber:[obj valueForKey:key]]) {
                [dict setObject: [self convertDictKeyToString:[obj valueForKey:key]] forKey:key];
            } else {
                [dict setObject:[obj valueForKey:key] forKey:key];
            }
        }
    }

    return [NSDictionary dictionaryWithDictionary:dict];
}

+(NSString *) stringFromDict: (NSDictionary*) dict
{
    if([NSJSONSerialization isValidJSONObject:dict]) {
        NSError *error;
        NSData *data = [NSJSONSerialization dataWithJSONObject:dict options:NSJSONWritingPrettyPrinted error:&error];
        if (error) {
            NSLog(@"error converting the dictionary to data, error: %@", error.localizedDescription);
            return nil;
        }
        NSString * jsonString = [[NSString alloc] initWithData:data
                                                      encoding:NSUTF8StringEncoding];
        return jsonString;
    }
    return nil;
}

+(NSDictionary<NSString *, NSNumber *> *) convertDictKeyToString: (NSDictionary *) dict {
    // Create a new NSMutableDictionary with the desired key type (NSString) and the same values (NSNumber)
    NSMutableDictionary<NSString *, NSNumber *> *newDictionary = [NSMutableDictionary dictionary];

    // Iterate through the original dictionary and convert keys to NSString
    for (NSNumber *key in dict) {
        NSString *stringKey = [key stringValue];
        NSNumber *value = dict[key];
        
        // Add the converted key and value to the new dictionary
        [newDictionary setObject:value forKey:stringKey];
    }

    // Now newDictionary is an NSDictionary<NSString *, NSNumber *>
    return newDictionary;

}

+ (BOOL)isAllKeysOfTypeNSNumber:(NSDictionary *)dictionary {
    for (id key in dictionary) {
        if (![key isKindOfClass:[NSNumber class]]) {
            return NO;
        }
    }
    return YES;
}

+ (ChoiceStyle *) mapUnityChoiceStyleToChoiceStyle:(UnityChoiceStyle *)unityChoiceStyle {
    ChoiceColor *lightModeColors = [ChoiceCMPUtils mapUnityChoiceColorToChoiceColor: unityChoiceStyle.lightModeColors];
    ChoiceColor *darkModeColors = nil;
    if (@available(iOS 12.0, *)) {
        darkModeColors = [self mapUnityChoiceColorToChoiceColor: unityChoiceStyle.darkModeColors];
    }
    ChoiceFont* boldFont = [[ChoiceFont alloc] init];
    if(![unityChoiceStyle.boldFont isKindOfClass:[NSNull class]]) {
        boldFont.fontName = unityChoiceStyle.boldFont;
    }
    ChoiceFont* regularFont = [[ChoiceFont alloc] init];;
    if(![unityChoiceStyle.regularFont isKindOfClass:[NSNull class]]) {
        regularFont.fontName = unityChoiceStyle.regularFont;
    }
    CMPUserInterfaceStyle* themeMode = [self mapThemeMode:unityChoiceStyle.themeMode];
    
    if (@available(iOS 12.0, *)) {
        ChoiceStyle *choiceStyle = [[ChoiceStyle alloc] initWithPreferredThemeMode:themeMode lightModeColors:lightModeColors darkModeColors:darkModeColors regularFont:regularFont boldFont:boldFont];
        return choiceStyle;
    } else {
        ChoiceStyle *choiceStyle = [[ChoiceStyle alloc] initWithLightModeColors:lightModeColors regularFont:regularFont boldFont:boldFont];
        return choiceStyle;
    }
}

+ (CMPUserInterfaceStyle *) mapThemeMode:(NSNumber* _Nullable)themeMode {
    if([themeMode isKindOfClass:[NSNull class]]) {
        return CMPUserInterfaceStyleAuto;
    }
    if([themeMode intValue] == 1){
        return CMPUserInterfaceStyleLight;
    } else if([themeMode intValue] == 2){
        return CMPUserInterfaceStyleDark;
    } else {
        return CMPUserInterfaceStyleAuto;
    }
}

+ (ChoiceColor * _Nullable) mapUnityChoiceColorToChoiceColor:(UnityChoiceColor * _Nullable) unityChoiceColor {
    ChoiceColor *choiceColor = [[ChoiceColor alloc] init];
    if (![unityChoiceColor.dividerColor isKindOfClass:[NSNull class]]) {
        choiceColor.dividerColor = unityChoiceColor.dividerColor;
    }
    if (![unityChoiceColor.tabBackgroundColor isKindOfClass:[NSNull class]]) {
        choiceColor.tabBackgroundColor = unityChoiceColor.tabBackgroundColor;
    }
    if (![unityChoiceColor.searchBarBackgroundColor isKindOfClass:[NSNull class]]) {
        choiceColor.searchBarBackgroundColor = unityChoiceColor.searchBarBackgroundColor;
    }
    if (![unityChoiceColor.searchBarForegroundColor isKindOfClass:[NSNull class]]) {
        choiceColor.searchBarForegroundColor = unityChoiceColor.searchBarForegroundColor;
    }
    if (![unityChoiceColor.infoButtonForegroundColor isKindOfClass:[NSNull class]]) {
        choiceColor.infoButtonForegroundColor = unityChoiceColor.infoButtonForegroundColor;
    }
    if (![unityChoiceColor.toggleActiveColor isKindOfClass:[NSNull class]]) {
        choiceColor.toggleActiveColor = unityChoiceColor.toggleActiveColor;
    }
    if (![unityChoiceColor.toggleInactiveColor isKindOfClass:[NSNull class]]) {
        choiceColor.toggleInactiveColor = unityChoiceColor.toggleInactiveColor;
    }
    if (![unityChoiceColor.globalBackgroundColor isKindOfClass:[NSNull class]]) {
        choiceColor.globalBackgroundColor = unityChoiceColor.globalBackgroundColor;
    }
    if (![unityChoiceColor.titleTextColor isKindOfClass:[NSNull class]]) {
        choiceColor.titleTextColor = unityChoiceColor.titleTextColor;
    }
    if (![unityChoiceColor.bodyTextColor isKindOfClass:[NSNull class]]) {
        choiceColor.bodyTextColor = unityChoiceColor.bodyTextColor;
    }
    if (![unityChoiceColor.tabTextColor isKindOfClass:[NSNull class]]) {
        choiceColor.tabTextColor = unityChoiceColor.tabTextColor;
    }
    if (![unityChoiceColor.menuTextColor isKindOfClass:[NSNull class]]) {
        choiceColor.menuTextColor = unityChoiceColor.menuTextColor;
    }
    if (![unityChoiceColor.linkTextColor isKindOfClass:[NSNull class]]) {
        choiceColor.linkTextColor = unityChoiceColor.linkTextColor;
    }
    if (![unityChoiceColor.buttonTextColor isKindOfClass:[NSNull class]]) {
        choiceColor.buttonTextColor = unityChoiceColor.buttonTextColor;
    }
    if (![unityChoiceColor.buttonDisabledTextColor isKindOfClass:[NSNull class]]) {
        choiceColor.buttonDisabledTextColor = unityChoiceColor.buttonDisabledTextColor;
    }
    if (![unityChoiceColor.buttonBackgroundColor isKindOfClass:[NSNull class]]) {
        choiceColor.buttonBackgroundColor = unityChoiceColor.buttonBackgroundColor;
    }
    if (![unityChoiceColor.buttonDisabledBackgroundColor isKindOfClass:[NSNull class]]) {
        choiceColor.buttonDisabledBackgroundColor = unityChoiceColor.buttonDisabledBackgroundColor;
    }
    return choiceColor;
}

+ (NSMutableDictionary *)changeKeyName:(NSString *)fromKey to:(NSString *)toKey inDictionary:(NSDictionary *)dictionary
{
    NSMutableDictionary *mutableDict = [dictionary mutableCopy];
    id value = [mutableDict objectForKey:fromKey];
    
    if (value != nil) {
        // Remove the old key-value pair
        [mutableDict removeObjectForKey:fromKey];

        // Add the value with the new key
        [mutableDict setObject:value forKey:toKey];
    }
    return mutableDict;
}

@end
