using System.Linq;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Models;
using Core;

namespace Infrastructure;

public class CheepRepository : ICheepRepository
{
    private readonly CheepDbContext _dbcontext;

    public CheepRepository(CheepDbContext dbcontext)
    {
        _dbcontext = dbcontext;
    }

    public async Task<IEnumerable<MessageDTO>> GetAllCheepsAsync()
    {
        var cheeps = await _dbcontext.Cheeps
            .Include(c => c.Author)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();

        // Map entity → DTO
        return cheeps.Select(c => new MessageDTO
        {
            Id = c.CheepId,
            Text = c.Text,
            AuthorName = c.Author.Name,
            TimeStamp = c.TimeStamp
        }).ToList();
    }

    public async Task<MessageDTO?> GetCheepByIdAsync(int id)
    {
        var cheep = await _dbcontext.Cheeps
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.CheepId == id);

        if (cheep == null) return null;

        return new MessageDTO
        {
            Id = cheep.CheepId,
            Text = cheep.Text,
            AuthorName = cheep.Author.Name
        };
    }

    public async Task<IEnumerable<MessageDTO>> GetAllCheepsFromAuthorAsync(string authorName)
    {
     var cheeps = await _dbcontext.Cheeps
            .Include(c => c.Author)
            .Where(c => c.Author.Name == authorName)  // ← Changed from .Email to .Name
            .OrderByDescending(c => c.TimeStamp)
           .ToListAsync();

     return cheeps.Select(c => new MessageDTO
        {
          Id = c.CheepId,
          Text = c.Text,
          AuthorName = c.Author.Name,
          TimeStamp = c.TimeStamp
        }).ToList();
    }


    public async Task StoreCheepAsync(MessageDTO message)
    {
        var author = await _dbcontext.Authors
            .FirstOrDefaultAsync(a => a.Name == message.AuthorName);

        if (author == null)
        {
            author = new Author
            {
                Name = message.AuthorName,
                Email = $"{message.AuthorName}@example.com"
            };
            _dbcontext.Authors.Add(author);
            await _dbcontext.SaveChangesAsync();
        }

        var cheepEntity = new Cheep
        {
            Text = message.Text,
            AuthorId = author.AuthorId,
            TimeStamp = DateTime.UtcNow
        };

        _dbcontext.Cheeps.Add(cheepEntity);
        await _dbcontext.SaveChangesAsync();
    }

    public async Task FollowUserAsync(string followerName, string followeeName)
{
    if (followerName == followeeName) return;

    var follower = await _dbcontext.Authors.FirstOrDefaultAsync(a => a.Name == followerName);
    if (follower == null)
    {
        follower = new Author
        {
            Name = followerName,
            Email = $"{followerName}@example.com" // or leave empty if not needed
        };
        _dbcontext.Authors.Add(follower);
        await _dbcontext.SaveChangesAsync(); // Need to save to get AuthorId
    }

    var followee = await _dbcontext.Authors.FirstOrDefaultAsync(a => a.Name == followeeName);
    if (followee == null)
    {
        // Optionally create followee too, or just skip/return
        // Most cases: followee already exists because they have cheeps
        return;
    }

    var existing = await _dbcontext.Follows
        .AnyAsync(f => f.FollowerId == follower.AuthorId && f.FolloweeId == followee.AuthorId);

    if (!existing)
    {
        _dbcontext.Follows.Add(new Follow
        {
            FollowerId = follower.AuthorId,
            FolloweeId = followee.AuthorId
        });
        await _dbcontext.SaveChangesAsync();
    }
}

    public async Task UnfollowUserAsync(string followerName, string followeeName)
    {
        var follower = await _dbcontext.Authors.FirstOrDefaultAsync(a => a.Name == followerName);
        var followee = await _dbcontext.Authors.FirstOrDefaultAsync(a => a.Name == followeeName);

        if (follower == null || followee == null) return;

        var follow = await _dbcontext.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == follower.AuthorId && f.FolloweeId == followee.AuthorId);

        if (follow != null)
        {
            _dbcontext.Follows.Remove(follow);
            await _dbcontext.SaveChangesAsync();
        }
    }

    public async Task<bool> IsFollowingAsync(string followerName, string followeeName)
    {
        var follower = await _dbcontext.Authors.FirstOrDefaultAsync(a => a.Name == followerName);
        var followee = await _dbcontext.Authors.FirstOrDefaultAsync(a => a.Name == followeeName);

        if (follower == null || followee == null) return false;

        return await _dbcontext.Follows
            .AnyAsync(f => f.FollowerId == follower.AuthorId && f.FolloweeId == followee.AuthorId);
    }

    public async Task<IEnumerable<MessageDTO>> GetTimelineForUserAsync(string username)
    {
        var author = await _dbcontext.Authors
            .Include(a => a.Following)
            .ThenInclude(f => f.Followee)
            .ThenInclude(f => f.Cheeps)
            .FirstOrDefaultAsync(a => a.Name == username);

        if (author == null) return Enumerable.Empty<MessageDTO>();

        var followedAuthorIds = author.Following.Select(f => f.Followee.AuthorId).ToList();
        followedAuthorIds.Add(author.AuthorId); // Include own cheeps

        var cheeps = await _dbcontext.Cheeps
            .Include(c => c.Author)
            .Where(c => followedAuthorIds.Contains(c.AuthorId))
            .OrderByDescending(c => c.TimeStamp)
            .Select(c => new MessageDTO
            {
                Id = c.CheepId,
                Text = c.Text,
                AuthorName = c.Author.Name,
                TimeStamp = c.TimeStamp
            })
            .ToListAsync();

        return cheeps;
    }

    public async Task<IReadOnlyList<string>> GetFollowingNamesAsync(string followerName)
{
    var author = await _dbcontext.Authors
        .Include(a => a.Following)
        .ThenInclude(f => f.Followee)
        .FirstOrDefaultAsync(a => a.Name == followerName);

    if (author == null) return Array.Empty<string>();

    return author.Following
        .Select(f => f.Followee.Name)
        .OrderBy(n => n)
        .ToList();
}
}
