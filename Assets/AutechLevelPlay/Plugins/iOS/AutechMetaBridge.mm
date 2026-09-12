// Autech LevelPlay Mediation - Meta Audience Network (FAN) iOS bridge.
//
// Meta requires setAdvertiserTrackingEnabled to be set BEFORE LevelPlay
// initializes. No mediation adapter wires it to the device's ATT status for you,
// so the package does it (see MetaAudienceNetwork.cs).
//
// FBAdSettings is resolved dynamically with NSClassFromString rather than linked,
// so this file compiles and runs whether or not the Meta adapter is in the build.
// Every entry point is a safe no-op when Meta is absent.

#import <Foundation/Foundation.h>

extern "C" {

// 1 when the Meta Audience Network SDK is present in this build.
int _autechMetaIsAvailable(void)
{
    if (NSClassFromString(@"FBAdSettings") != nil) {
        return 1;
    }
    return 0;
}

// Returns 1 when the flag was actually applied, 0 when Meta is absent or the
// selector is unavailable on the linked SDK version.
int _autechMetaSetAdvertiserTrackingEnabled(int enabled)
{
    Class settings = NSClassFromString(@"FBAdSettings");
    if (settings == nil) {
        return 0;
    }

    SEL selector = NSSelectorFromString(@"setAdvertiserTrackingEnabled:");
    if (![settings respondsToSelector:selector]) {
        return 0;
    }

    NSMethodSignature* signature = [settings methodSignatureForSelector:selector];
    if (signature == nil) {
        return 0;
    }

    NSInvocation* invocation = [NSInvocation invocationWithMethodSignature:signature];
    [invocation setSelector:selector];
    [invocation setTarget:settings];

    BOOL value = NO;
    if (enabled != 0) {
        value = YES;
    }
    [invocation setArgument:&value atIndex:2];
    [invocation invoke];
    return 1;
}

}
