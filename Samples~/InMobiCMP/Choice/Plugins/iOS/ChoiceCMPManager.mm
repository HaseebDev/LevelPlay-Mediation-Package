
//
//  ChoiceCMPManager.m
//  InMobi
//
//  Copyright (c) 2023 InMobi. All rights reserved.
//

#import "ChoiceCMPManager.h"
#import <InMobiCMP/InMobiCMP-Swift.h>
#import <Foundation/Foundation.h>
#import "ChoiceCMPUtils.h"
#import "UnityChoiceStyle.h"

#ifdef __cplusplus
extern "C" {
#endif
    // life cycle management
    void UnityPause(int pause);
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
#ifdef __cplusplus
}
#endif

@implementation ChoiceCMPManager

// Manager to be used for methods that do not require a specific adunit to operate on.
+ (ChoiceCMPManager*)shared
{
    static ChoiceCMPManager* sharedManager = nil;

    if (!sharedManager)
        sharedManager = [[ChoiceCMPManager alloc] init];

    return sharedManager;
}

+ (void)sendUnityEvent:(NSString*)eventName withArgs:(NSString*)args
{
    UnitySendMessage("ChoiceCMPManager", eventName.UTF8String, args.UTF8String);
}

- (void)sendUnityEvent:(NSString*)eventName
{
    [[self class] sendUnityEvent:eventName withArgs:@""];
}

- (void)startChoiceWithPCode:(NSString *)pCode unityChoiceStyle:(UnityChoiceStyle *)unityChoiceStyle shouldDisplayIDFA:(BOOL)shouldDisplayIDFA {
    ChoiceStyle *choiceStyleResource = [ChoiceCMPUtils mapUnityChoiceStyleToChoiceStyle: unityChoiceStyle];
    [[ChoiceCmp shared] startChoiceWithPcode: pCode delegate: self ccpaDelegate: self gbcDelegate: self shouldDisplayIDFA: shouldDisplayIDFA style: choiceStyleResource];
}

- (void)showCCPAWithPCode:(NSString *)pCode {
    [[ChoiceCmp shared] showCCPAWithCcpaDelegate:self];
}

- (void)getGDPRData {
    return  [[ChoiceCmp shared] getGDPRDataWithCompletion:^(GDPRData * gdprData) {
        NSString *tcDataStr = [self getGDPRDataString:gdprData];
            
        if (tcDataStr != nil)
            [[self class] sendUnityEvent:@"EmitCMPDidReceiveGDPRDataEvent" withArgs:tcDataStr];
         else
             [self sendUnityEvent:@"EmitCMPDidReceiveGDPRDataEvent"];
    }];
}

- (void)forceDisplayUI {
    [[ChoiceCmp shared] forceDisplayUI];
}

- (void)showUSRegulations {
    [[ChoiceCmp shared] showUSRegulationsWithCcpaDelegate:self];
}

- (void)showGoogleBasicConsent {
    [[ChoiceCmp shared] showGoogleBasicConsentWithDelegate:self];
}

- (NSString*) getGDPRDataString:(GDPRData*) gdprData {
    NSDictionary *dictionary = [ChoiceCMPUtils dictionaryFromObject:gdprData];
    
    NSMutableDictionary *mutableDict = [ChoiceCMPUtils changeKeyName:@"doesGdprApply" to:@"gdprApplies" inDictionary:dictionary];
        
    NSDictionary *vendorDict = [ChoiceCMPUtils dictionaryFromObject:gdprData.vendor];
    NSDictionary *purposeDict = [ChoiceCMPUtils dictionaryFromObject:gdprData.purpose];
    
    NSDictionary *publisherDict = [ChoiceCMPUtils dictionaryFromObject:gdprData.publisher];
    NSMutableDictionary *mutablePublisherDict = [ChoiceCMPUtils changeKeyName:@"vendorID" to:@"vendorId" inDictionary:publisherDict];
    ConsentAndLI *customPurpose = [publisherDict objectForKey:@"customPurpose"];
    NSDictionary *customPurposeDict = [ChoiceCMPUtils dictionaryFromObject:customPurpose];
    
    if(customPurposeDict != nil)
        [mutablePublisherDict setObject:customPurposeDict forKey:@"customPurpose"];
    else
        [mutableDict setObject:[[NSDictionary alloc ]init] forKey:@"customPurpose"];
    
    if (vendorDict != nil)
        [mutableDict setObject:vendorDict forKey:@"vendor"];
    else
        [mutableDict setObject:[[NSDictionary alloc ]init] forKey:@"vendor"];
    
    if (purposeDict != nil)
        [mutableDict setObject:purposeDict forKey:@"purpose"];
    else
        [mutableDict setObject:[[NSDictionary alloc ]init] forKey:@"purpose"];
    
    if (mutablePublisherDict != nil)
        [mutableDict setObject:mutablePublisherDict forKey:@"publisher"];
    else
        [mutableDict setObject:[[NSDictionary alloc ]init] forKey:@"publisher"];

    NSString *tcDataStr = [ChoiceCMPUtils stringFromDict:mutableDict];
    return tcDataStr;
}

- (void)setUserLoginOrSubscriptionStatus:(bool)status {
    [[ChoiceCmp shared] setUserLoginOrSubscriptionStatus:status];
}

- (NSString*)getSDKVersion {
    return [ChoiceCmp shared].sdkVersion;
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - ChoiceCmpDelegate

- (void)cmpDidErrorWithError:(NSError * _Nonnull)error {
    [[self class] sendUnityEvent:@"EmitCMPDidErrorEvent" withArgs:error.localizedDescription];
}

- (void)cmpDidLoadWithInfo:(PingResponse * _Nonnull)info {
    NSDictionary *dict = [ChoiceCMPUtils dictionaryFromObject:info];
    
    NSMutableDictionary *mutableDict = [ChoiceCMPUtils changeKeyName:@"doesGdprApply" to:@"gdprApplies" inDictionary:dict];
    
    NSString *pingResponseStr = [ChoiceCMPUtils stringFromDict:mutableDict];
    
    if (pingResponseStr != nil)
        [[self class] sendUnityEvent:@"EmitCMPDidLoadEvent" withArgs:pingResponseStr];
     else
         [self sendUnityEvent:@"EmitCMPDidLoadEvent"];
}

- (void)didReceiveAdditionalConsentWithAcData:(ACData * _Nonnull)acData updated:(BOOL)updated {
    NSDictionary *dict = [ChoiceCMPUtils dictionaryFromObject:acData];
    NSString *acDataStr = [ChoiceCMPUtils stringFromDict:dict];
    
    if (acDataStr != nil)
        [[self class] sendUnityEvent:@"EmitCMPDidReceiveAdditionalConsentEvent" withArgs:acDataStr];
     else
         [self sendUnityEvent:@"EmitCMPDidReceiveAdditionalConsentEvent"];
    
}

- (void)didReceiveIABVendorConsentWithGdprData:(GDPRData * _Nonnull)gdprData updated:(BOOL)updated {
    
    NSString *tcDataStr = [self getGDPRDataString:gdprData];
        
    if (tcDataStr != nil)
        [[self class] sendUnityEvent:@"EmitCMPDidReceiveIABVendorConsentEvent" withArgs:tcDataStr];
     else
         [self sendUnityEvent:@"EmitCMPDidReceiveIABVendorConsentEvent"];
}

- (void)didReceiveNonIABVendorConsentWithNonIabData:(NonIABData * _Nonnull)nonIabData updated:(BOOL)updated {
    NSDictionary *dict = [ChoiceCMPUtils dictionaryFromObject:nonIabData];
    NSString *nonIabDataStr = [ChoiceCMPUtils stringFromDict:dict];
    
    if (nonIabDataStr != nil)
        [[self class] sendUnityEvent:@"EmitCMPDidReceiveNonIABVendorConsentEvent" withArgs:nonIabDataStr];
     else
         [self sendUnityEvent:@"EmitCMPDidReceiveNonIABVendorConsentEvent"];
}


- (void)didReceiveGoogleBasicConsentChangeWithConsents:(GoogleBasicConsents *)consents {
    NSDictionary *dict = [ChoiceCMPUtils dictionaryFromObject:consents];
    NSString *consentsStr = [ChoiceCMPUtils stringFromDict:dict];
    
    if (consentsStr != nil)
        [[self class] sendUnityEvent:@"EmitCMPDidReceiveGoogleBasicConsentEvent" withArgs:consentsStr];
     else
         [self sendUnityEvent:@"EmitCMPDidReceiveGoogleBasicConsentEvent"];
}

- (void)didReceiveUSRegulationsConsentWithUsRegData:(USRegulationsData *)usRegData {
    NSDictionary *dict = [ChoiceCMPUtils dictionaryFromObject:usRegData];
    NSString *usRegStr = [ChoiceCMPUtils stringFromDict:dict];
    
    if (usRegStr != nil)
        [[self class] sendUnityEvent:@"EmitCMPDidReceiveUSRegulationsConsentEvent" withArgs:usRegStr];
     else
         [self sendUnityEvent:@"EmitCMPDidReceiveUSRegulationsConsentEvent"];
}

- (void)userDidMoveToOtherState {
    [self sendUnityEvent:@"EmitUserDidMoveToOtherStateEvent"];
}

- (void)cmpUIStatusChangedWithInfo:(DisplayInfo * _Nonnull)info {
    NSDictionary *dict = [ChoiceCMPUtils dictionaryFromObject:info];
    NSString *infoStr = [ChoiceCMPUtils stringFromDict:dict];
    
    if (infoStr != nil)
        [[self class] sendUnityEvent:@"EmitCMPUIStatusChangedEvent" withArgs:infoStr];
     else
         [self sendUnityEvent:@"EmitCMPUIStatusChangedEvent"];
}


- (void)didReceiveActionButtonTapWithAction:(enum ActionButtons)action {
    [[self class] sendUnityEvent:@"EmitCMPActionButtonTapEvent" withArgs:[NSString stringWithFormat:@"%ld", (long)action]];
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - CCPADelegate
- (void)didReceiveCCPAConsentWithString:(NSString * _Nonnull)string {
    [[self class] sendUnityEvent:@"EmitCMPDidReceiveCCPAConsentEvent" withArgs:string];
}

@end
