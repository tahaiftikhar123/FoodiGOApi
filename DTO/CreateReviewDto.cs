namespace FoodiGOAPI.DTO
{
    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }


    public class ReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = "";

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public string? AdminReply { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class ReplyReviewDto
    {
        public string Reply { get; set; } = "";
    }

}
