//
//  InmobiCMPColor.h
//  UnityFramework
//
//  Created by vettrivel.a on 16/05/24.
//

#import <Foundation/Foundation.h>

@interface UnityChoiceColor : NSObject

@property NSString* _Nullable dividerColor;
@property NSString* _Nullable tabBackgroundColor;
@property NSString* _Nullable searchBarBackgroundColor;
@property NSString* _Nullable searchBarForegroundColor;
@property NSString* _Nullable infoButtonForegroundColor;
@property NSString* _Nullable toggleActiveColor;
@property NSString* _Nullable toggleInactiveColor;
@property NSString* _Nullable globalBackgroundColor;
@property NSString* _Nullable titleTextColor;
@property NSString* _Nullable bodyTextColor;
@property NSString* _Nullable tabTextColor;
@property NSString* _Nullable menuTextColor;
@property NSString* _Nullable linkTextColor;
@property NSString* _Nullable buttonTextColor;
@property NSString* _Nullable buttonDisabledTextColor;
@property NSString* _Nullable buttonBackgroundColor;
@property NSString* _Nullable buttonDisabledBackgroundColor;

- (instancetype _Nullable )initWithJSONDict:(NSDictionary *_Nullable)JSONDictionary;

@end
