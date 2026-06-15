//
//  InmobiCMPStyle.h
//  UnityFramework
//
//  Created by vettrivel.a on 16/05/24.
//

#import "UnityChoiceColor.h"
#import <Foundation/Foundation.h>

@interface UnityChoiceStyle : NSObject

@property NSNumber* _Nullable themeMode;
@property UnityChoiceColor* _Nullable lightModeColors;
@property UnityChoiceColor* _Nullable darkModeColors;
@property NSString* _Nullable boldFont;
@property NSString* _Nullable regularFont;


- (instancetype _Nullable )initWithJSONString:(NSString *_Nullable)JSONString;

@end
