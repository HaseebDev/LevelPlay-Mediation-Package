
//
//  ChoiceCMPManager.h
//  InMobi
//
//  Copyright (c) 2023 InMobi. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <InMobiCMP/InMobiCMP-Swift.h>
#import "UnityChoiceStyle.h"


@interface ChoiceCMPManager : NSObject <ChoiceCmpDelegate, CCPADelegate, GoogleBasicConsentDelegate>

+ (ChoiceCMPManager*) shared;

+ (void)sendUnityEvent:(NSString*)eventName withArgs:(NSString*)args;

- (void)sendUnityEvent:(NSString*)eventName;

- (void)startChoiceWithPCode:(NSString*)pCode unityChoiceStyle:(UnityChoiceStyle*)unityChoiceStyle shouldDisplayIDFA:(BOOL)shouldDisplayIDFA;

- (void)showCCPAWithPCode:(NSString*)pCode;

- (void)forceDisplayUI;

-(void)showUSRegulations;

-(void)showGoogleBasicConsent;

-(void)getGDPRData;

-(void)setUserLoginOrSubscriptionStatus:(bool)status;

-(NSString*)getSDKVersion;

@end
