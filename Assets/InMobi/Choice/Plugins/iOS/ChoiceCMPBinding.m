
//
//  ChoiceCMPBinding.m
//
//  Copyright (c) 2023 InMobi. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>
#import "ChoiceCMPManager.h"
#import "ChoiceCMPUtils.h"
#import "UnityChoiceStyle.h"


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - APIs
void _StartChoice(const char* pCode, const char* choiceStyle, bool shouldDisplayIDFA) {
    NSString* code = GetStringParam(pCode);
    NSString* choiceStyleJsonString = GetStringParam(choiceStyle);
    UnityChoiceStyle* unityChoiceStyle = [[UnityChoiceStyle alloc] initWithJSONString:choiceStyleJsonString];

    [[ChoiceCMPManager shared] startChoiceWithPCode: code unityChoiceStyle:unityChoiceStyle shouldDisplayIDFA:shouldDisplayIDFA];
}

void _ShowCCPA(const char* pCode) {
    NSString* code = GetStringParam(pCode);
    [[ChoiceCMPManager shared] showCCPAWithPCode: code];
}

void _ForceDisplayUI() {
    [[ChoiceCMPManager shared] forceDisplayUI];
}

void _GetGDPRData() {
    [[ChoiceCMPManager shared] getGDPRData];
}

void _ShowUSRegulation() {
    [[ChoiceCMPManager shared] showUSRegulations];
}

void _ShowGoogleBasicConsent() {
    [[ChoiceCMPManager shared] showGoogleBasicConsent];
}

void _SetUserLoginOrSubscriptionStatus(const bool isUserLoggedInOrSubscribed) {
    [[ChoiceCMPManager shared] setUserLoginOrSubscriptionStatus:isUserLoggedInOrSubscribed];
}

const char* _GetSDKVersion() {
    return [ChoiceCMPUtils cStringCopy:[[ChoiceCMPManager shared] getSDKVersion]];
}

