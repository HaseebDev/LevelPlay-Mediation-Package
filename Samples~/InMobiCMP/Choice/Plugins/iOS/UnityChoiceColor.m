//
//  JSONDictionary[@"m
//  UnityFramework
//
//  Created by vettrivel.a on 16/05/24.
//

#import "UnityChoiceColor.h"
#import <Foundation/Foundation.h>
#import "ChoiceCMPUtils.h"

@implementation UnityChoiceColor : NSObject

- (instancetype)init
{
    self = [super init];
    return self;
}

- (instancetype _Nullable)initWithJSONDict:(NSDictionary * _Nullable)JSONDictionary {
    self = [super init];
    if (self && ![JSONDictionary isKindOfClass:[NSNull class]]) {
        self.dividerColor = JSONDictionary[@"dividerColor"];
        self.tabBackgroundColor = JSONDictionary[@"tabBackgroundColor"];
        self.searchBarBackgroundColor = JSONDictionary[@"searchBarBackgroundColor"];
        self.searchBarForegroundColor = JSONDictionary[@"searchBarForegroundColor"];
        self.infoButtonForegroundColor = JSONDictionary[@"infoButtonForegroundColor"];
        self.toggleActiveColor = JSONDictionary[@"toggleActiveColor"];
        self.toggleInactiveColor = JSONDictionary[@"toggleInactiveColor"];
        self.globalBackgroundColor = JSONDictionary[@"globalBackgroundColor"];
        self.titleTextColor = JSONDictionary[@"titleTextColor"];
        self.bodyTextColor = JSONDictionary[@"bodyTextColor"];
        self.tabTextColor = JSONDictionary[@"tabTextColor"];
        self.menuTextColor = JSONDictionary[@"menuTextColor"];
        self.linkTextColor = JSONDictionary[@"linkTextColor"];
        self.buttonTextColor = JSONDictionary[@"buttonTextColor"];
        self.buttonDisabledTextColor = JSONDictionary[@"buttonDisabledTextColor"];
        self.buttonBackgroundColor = JSONDictionary[@"buttonBackgroundColor"];
        self.buttonDisabledBackgroundColor = JSONDictionary[@"buttonDisabledBackgroundColor"];
    }
    return self;
}

@end
