namespace LECOMS.Data.DTOs.Gamification
{
    public class GamificationEventDTO
    {
        /// <summary>
        /// Action: "CompleteLesson", "FinishCourse", "PurchaseProduct", "WriteReview"...
        /// phải trùng với EarnRule.Action
        /// </summary>
        public string Action { get; set; } = null!;

        /// <summary>Id liên quan: lessonId, courseId, orderId, reviewId...</summary>
        public string? ReferenceId { get; set; }
    }
}
