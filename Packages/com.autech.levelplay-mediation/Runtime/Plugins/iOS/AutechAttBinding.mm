// Autech LevelPlay Mediation — App Tracking Transparency binding.
// Status values mirror ATTrackingManagerAuthorizationStatus:
// 0 = NotDetermined, 1 = Restricted, 2 = Denied, 3 = Authorized.

#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

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
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            // Result is read by polling _autechAttGetStatus from C#.
        }];
    }
}

}
