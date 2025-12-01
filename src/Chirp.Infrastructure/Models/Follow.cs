namespace Infrastructure.Models;

public class Follow
{
    public int FollowerId { get; set; }
    public Author Follower { get; set; } = null!;

    public int FolloweeId { get; set; }
    public Author Followee { get; set; } = null!;
}