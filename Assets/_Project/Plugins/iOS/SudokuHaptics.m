// Impact haptics for iOS.
//
// The engine has no cross-platform impact API - Handheld.Vibrate is one
// half-second buzz - so the only way to make a placement feel different from a
// mistake is to talk to UIImpactFeedbackGenerator directly. Called from
// Sudoku.Game.Audio.Haptics.
//
// The generators are kept alive between taps on purpose: a freshly allocated
// generator answers its first impact late, and preparing again straight after
// firing keeps the taptic engine warm for the next digit, which on a Sudoku
// board is usually seconds away.

#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *_sudokuLight = nil;
static UIImpactFeedbackGenerator *_sudokuMedium = nil;
static UIImpactFeedbackGenerator *_sudokuHeavy = nil;

void _SudokuHapticsPrepare(void)
{
    if (@available(iOS 10.0, *))
    {
        if (_sudokuLight == nil)
            _sudokuLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        if (_sudokuMedium == nil)
            _sudokuMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        if (_sudokuHeavy == nil)
            _sudokuHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];

        [_sudokuLight prepare];
        [_sudokuMedium prepare];
        [_sudokuHeavy prepare];
    }
}

// style follows UIImpactFeedbackStyle: 0 light, 1 medium, 2 heavy.
void _SudokuHapticsImpact(int style)
{
    if (@available(iOS 10.0, *))
    {
        _SudokuHapticsPrepare();

        UIImpactFeedbackGenerator *generator = _sudokuLight;
        if (style == 1) generator = _sudokuMedium;
        else if (style >= 2) generator = _sudokuHeavy;

        [generator impactOccurred];
        [generator prepare];
    }
}
