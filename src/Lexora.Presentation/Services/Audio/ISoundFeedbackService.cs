namespace Lexora.Presentation.Services;

public interface ISoundFeedbackService
{
    void PlayCardFlipSound();
    void PlaySoftTapSound();
    void PlayReviewCompleteSound();
    void PlayStreakSound();
    void PlaySuccessSound();
    void PlayErrorSound();
}
