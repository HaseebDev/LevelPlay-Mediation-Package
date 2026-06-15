//
//  InmobiCMPStyle.m
//  UnityFramework
//
//  Created by vettrivel.a on 16/05/24.
//

#import "UnityChoiceStyle.h"
#import <Foundation/Foundation.h>

@implementation UnityChoiceStyle : NSObject

- (instancetype)init
{
    self = [super init];
    return self;
}

- (instancetype)initWithJSONString:(NSString *)JSONString
{
    self = [super init];
    if (self) {

        NSError *error = nil;
        NSData *JSONData = [JSONString dataUsingEncoding:NSUTF8StringEncoding];
        NSDictionary *JSONDictionary = [NSJSONSerialization JSONObjectWithData:JSONData options:0 error:&error];

        self.themeMode = JSONDictionary[@"themeMode"];
        self.boldFont = JSONDictionary[@"boldFont"];
        self.regularFont = JSONDictionary[@"regularFont"];
        self.lightModeColors = [[UnityChoiceColor alloc] initWithJSONDict:JSONDictionary[@"lightModeColors"]];
        self.darkModeColors = [[UnityChoiceColor alloc] initWithJSONDict:JSONDictionary[@"darkModeColors"]];
    }
    return self;
}

@end

