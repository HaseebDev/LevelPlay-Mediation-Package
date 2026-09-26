// Autech LevelPlay Mediation - App Tracking Transparency binding.
// Status values mirror ATTrackingManagerAuthorizationStatus:
// 0 = NotDetermined, 1 = Restricted, 2 = Denied, 3 = Authorized.
//
// iOS only presents the ATT prompt while the app is ACTIVE. A request made while
// the app is inactive (another system alert on screen, an incoming call, Control
// Center pulled down) is dropped silently: no prompt, and the status stays
// NotDetermined. A single fire-and-forget request therefore loses the prompt for
// the whole session whenever anything interrupts at that moment.
//
// So a request here is ARMED rather than fired once: it asks immediately if the
// app is active, and asks again every time the app becomes active, until the
// status is resolved. The observer removes itself once there is an answer.

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

static void AutechAttRequestIfActive(void);

// Plain target/selector observer rather than the block API, so the lifetime is
// explicit and correct whether this file compiles with or without ARC.
@interface AutechAttActiveObserver : NSObject
@end

@implementation AutechAttActiveObserver
- (void)applicationDidBecomeActive:(NSNotification *)notification
{
    AutechAttRequestIfActive();
}
@end

static AutechAttActiveObserver *s_activeObserver = nil;
static BOOL s_observing = NO;

static void AutechAttStartObserving(void)
{
    if (s_observing) {
        return;
    }
    if (s_activeObserver == nil) {
        s_activeObserver = [[AutechAttActiveObserver alloc] init];
    }
    [[NSNotificationCenter defaultCenter] addObserver:s_activeObserver
                                             selector:@selector(applicationDidBecomeActive:)
                                                 name:UIApplicationDidBecomeActiveNotification
                                               object:nil];
    s_observing = YES;
}

static void AutechAttStopObserving(void)
{
    if (!s_observing) {
        return;
    }
    [[NSNotificationCenter defaultCenter] removeObserver:s_activeObserver
                                                    name:UIApplicationDidBecomeActiveNotification
                                                  object:nil];
    s_observing = NO;
}

// Ask now if it can actually be shown; otherwise leave it to the observer.
// Must run on the main thread (UIApplication state and ATT presentation).
static void AutechAttRequestIfActive(void)
{
    if (@available(iOS 14, *)) {
        if ([ATTrackingManager trackingAuthorizationStatus] != ATTrackingManagerAuthorizationStatusNotDetermined) {
            AutechAttStopObserving();
            return;
        }

        if ([UIApplication sharedApplication].applicationState != UIApplicationStateActive) {
            // iOS would drop it. The didBecomeActive observer retries.
            return;
        }

        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            // A dropped request completes with NotDetermined; keep the observer
            // armed so the next return to active asks again.
            if (status != ATTrackingManagerAuthorizationStatusNotDetermined) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    AutechAttStopObserving();
                });
            }
        }];
    } else {
        AutechAttStopObserving();
    }
}

extern "C" {

int _autechAttGetStatus(void)
{
    if (@available(iOS 14, *)) {
        return (int)[ATTrackingManager trackingAuthorizationStatus];
    }
    // Pre-iOS 14 has no ATT; treat as authorized (legacy behaviour).
    return 3;
}

void _autechAttRequest(void)
{
    if (@available(iOS 14, *)) {
        dispatch_async(dispatch_get_main_queue(), ^{
            AutechAttStartObserving();
            AutechAttRequestIfActive();
        });
    }
}

}
