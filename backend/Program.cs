using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("12345678901234567890123456789012"))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite("Data Source=app.db"));

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
});

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var users = new List<User>();
var dietPlans = new List<DietPlan>();
var exercises = new List<Exercise>();
var socialPosts = new List<SocialPost>();

string HashPassword(string password) => password;

string GenerateJwtToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("UserId", user.Id.ToString())
    };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("12345678901234567890123456789012"));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddHours(12),
        signingCredentials: creds);
    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/register", async (User user, DataContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Username == user.Username))
        return Results.BadRequest("Username already exists");
    user.PasswordHash = HashPassword(user.PasswordHash);
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPost("/login", async (User login, DataContext db) =>
{
    var hash = HashPassword(login.PasswordHash);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == login.Username && u.PasswordHash == hash);
    if (user == null) return Results.Unauthorized();
    var token = GenerateJwtToken(user);
    return Results.Ok(new { token, role = user.Role });
});

app.MapGet("/me", async (ClaimsPrincipal user, DataContext db) =>
{
    var username = user.Identity?.Name;
    var u = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (u == null) return Results.NotFound();
    return Results.Ok(u);
}).RequireAuthorization();

app.MapPost("/promote-me-admin", async (ClaimsPrincipal user, DataContext db) =>
{
    var username = user.Identity?.Name;
    var u = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (u == null) return Results.NotFound();
    u.Role = "Admin";
    await db.SaveChangesAsync();
    return Results.Ok(new { message = $"{u.Username} promoted to Admin." });
}).RequireAuthorization();

app.MapPut("/me", async (ClaimsPrincipal user, User updated, DataContext db) =>
{
    var username = user.Identity?.Name;
    var u = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (u == null) return Results.NotFound();
    u.FullName = updated.FullName;
    u.Address = updated.Address;
    u.ContactNumber = updated.ContactNumber;
    u.Email = updated.Email;
    u.AvatarUrl = updated.AvatarUrl;
    await db.SaveChangesAsync();
    return Results.Ok(u);
}).RequireAuthorization();

app.MapGet("/dietplan", async (ClaimsPrincipal user, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var plan = await db.DietPlans.FirstOrDefaultAsync(d => d.UserId == userId);
    return plan is null ? Results.NotFound() : Results.Ok(plan);
}).RequireAuthorization();

app.MapPost("/dietplan", async (ClaimsPrincipal user, DietPlan plan, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var u = await db.Users.FindAsync(userId);
    if (u == null) return Results.NotFound();
    plan.UserId = userId;
    db.DietPlans.Add(plan);
    await db.SaveChangesAsync();
    return Results.Ok(plan);
}).RequireAuthorization();

app.MapPut("/dietplan/{userId}", async (ClaimsPrincipal user, int userId, DietPlan plan, DataContext db) =>
{
    var isAdmin = user.IsInRole("Admin");
    var currentUserId = int.Parse(user.FindFirst("UserId")!.Value);
    if (isAdmin || currentUserId == userId)
    {
        var existing = await db.DietPlans.FirstOrDefaultAsync(d => d.UserId == userId);
        if (existing == null) return Results.NotFound();
        existing.Calories = plan.Calories;
        existing.Macros = plan.Macros;
        existing.Notes = plan.Notes;
        await db.SaveChangesAsync();
        return Results.Ok(existing);
    }
    return Results.Forbid();
}).RequireAuthorization();

app.MapGet("/exercises", async (ClaimsPrincipal user, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var userExercises = await db.Exercises.Where(e => e.UserId == userId).ToListAsync();
    return Results.Ok(userExercises);
}).RequireAuthorization();

app.MapPost("/exercises", async (ClaimsPrincipal user, Exercise exercise, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    exercise.UserId = userId;
    db.Exercises.Add(exercise);
    await db.SaveChangesAsync();
    return Results.Ok(exercise);
}).RequireAuthorization();

app.MapPut("/exercises/{userId}/{exerciseId}", async (ClaimsPrincipal user, int userId, int exerciseId, Exercise updated, DataContext db) =>
{
    var isAdmin = user.IsInRole("Admin");
    var currentUserId = int.Parse(user.FindFirst("UserId")!.Value);
    if (isAdmin || currentUserId == userId)
    {
        var ex = await db.Exercises.FirstOrDefaultAsync(e => e.UserId == userId && e.Id == exerciseId);
        if (ex == null) return Results.NotFound();
        ex.Name = updated.Name;
        ex.Description = updated.Description;
        ex.MuscleGroup = updated.MuscleGroup;
        ex.Equipment = updated.Equipment;
        ex.Difficulty = updated.Difficulty;
        ex.Sets = updated.Sets;
        ex.Repetitions = updated.Repetitions;
        ex.RestTimeSeconds = updated.RestTimeSeconds;
        ex.Instructions = updated.Instructions;
        ex.ImageUrl = updated.ImageUrl;
        ex.TimeLimitMinutes = updated.TimeLimitMinutes;
        ex.Notes = updated.Notes;
        ex.Weight = updated.Weight;
        ex.Day = updated.Day;
        await db.SaveChangesAsync();
        return Results.Ok(ex);
    }
    return Results.Forbid();
}).RequireAuthorization();

app.MapDelete("/exercises/{userId}/{exerciseId}", async (ClaimsPrincipal user, int userId, int exerciseId, DataContext db) =>
{
    var isAdmin = user.IsInRole("Admin");
    var currentUserId = int.Parse(user.FindFirst("UserId")!.Value);
    if (isAdmin || currentUserId == userId)
    {
        var ex = await db.Exercises.FirstOrDefaultAsync(e => e.UserId == userId && e.Id == exerciseId);
        if (ex == null) return Results.NotFound();
        db.Exercises.Remove(ex);
        await db.SaveChangesAsync();
        return Results.Ok();
    }
    return Results.Forbid();
}).RequireAuthorization();

app.MapGet("/social", async (DataContext db) =>
    Results.Ok(await db.SocialPosts
        .Include(p => p.User)
        .Include(p => p.Likes)
        .Include(p => p.Comments)
            .ThenInclude(c => c.User)
        .OrderByDescending(p => p.Pinned)
        .ThenByDescending(p => p.Timestamp)
        .Select(p => new {
            id = p.Id,
            content = p.Content,
            achievement = p.Achievement,
            imageUrl = p.ImageUrl,
            tag = p.Tag,
            pinned = p.Pinned,
            timestamp = p.Timestamp,
            user = new {
                id = p.User.Id,
                username = p.User.Username,
                avatarUrl = p.User.AvatarUrl,
                role = p.User.Role
            },
            likes = p.Likes.Select(l => new { userId = l.UserId }),
            comments = p.Comments.Count
        })
        .ToListAsync()));

app.MapPost("/social", async (HttpRequest request, ClaimsPrincipal user, DataContext db) =>
{
    var form = await request.ReadFormAsync();
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var content = form["content"].ToString();
    var achievement = form["achievement"].ToString();
    var tag = form["tag"].ToString();
    string? imageUrl = null;
    var file = form.Files["image"];
    if (file != null && file.Length > 0)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            return Results.BadRequest("Only .jpg, .jpeg, .png files are allowed.");
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "post-images");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var fileName = $"post_{userId}_{Guid.NewGuid().ToString().Substring(0,8)}{ext}";
        var filePath = Path.Combine(dir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        imageUrl = $"/post-images/{fileName}";
    }
    var post = new SocialPost
    {
        UserId = userId,
        Content = content,
        Achievement = achievement,
        Tag = tag,
        ImageUrl = imageUrl,
        Timestamp = DateTime.UtcNow
    };
    db.SocialPosts.Add(post);
    await db.SaveChangesAsync();
    return Results.Ok(post);
}).Accepts<IFormFile>("multipart/form-data").RequireAuthorization();

app.MapPut("/social/{postId}", async (HttpRequest request, ClaimsPrincipal user, int postId, DataContext db) =>
{
    var form = await request.ReadFormAsync();
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var isAdmin = user.IsInRole("Admin") || user.IsInRole("Trainer");
    var post = await db.SocialPosts.FindAsync(postId);
    if (post == null) return Results.NotFound();
    if (post.UserId != userId && !isAdmin) return Results.Forbid();
    post.Content = form["content"].ToString();
    post.Achievement = form["achievement"].ToString();
    post.Tag = form["tag"].ToString();
    var file = form.Files["image"];
    if (file != null && file.Length > 0)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            return Results.BadRequest("Only .jpg, .jpeg, .png files are allowed.");
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "post-images");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var fileName = $"post_{userId}_{Guid.NewGuid().ToString().Substring(0,8)}{ext}";
        var filePath = Path.Combine(dir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        post.ImageUrl = $"/post-images/{fileName}";
    }
    await db.SaveChangesAsync();
    return Results.Ok(post);
}).Accepts<IFormFile>("multipart/form-data").RequireAuthorization();

app.MapDelete("/social/{postId}", async (ClaimsPrincipal user, int postId, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var isAdmin = user.IsInRole("Admin") || user.IsInRole("Trainer");
    var post = await db.SocialPosts.FindAsync(postId);
    if (post == null) return Results.NotFound();
    if (post.UserId != userId && !isAdmin) return Results.Forbid();
    db.SocialPosts.Remove(post);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/social/{postId}/like", async (ClaimsPrincipal user, int postId, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var like = await db.SocialLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
    if (like != null)
    {
        db.SocialLikes.Remove(like);
    }
    else
    {
        db.SocialLikes.Add(new SocialLike { PostId = postId, UserId = userId, Timestamp = DateTime.UtcNow });
    }
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/social/{postId}/pin", async (ClaimsPrincipal user, int postId, DataContext db) =>
{
    if (!user.IsInRole("Admin") && !user.IsInRole("Trainer")) return Results.Forbid();
    var post = await db.SocialPosts.FindAsync(postId);
    if (post == null) return Results.NotFound();
    post.Pinned = !post.Pinned;
    await db.SaveChangesAsync();
    return Results.Ok(post);
}).RequireAuthorization();

app.MapPost("/social/{postId}/comments", async (HttpRequest request, ClaimsPrincipal user, int postId, DataContext db) =>
{
    var form = await request.ReadFormAsync();
    Console.WriteLine($"Form files count: {form.Files.Count}");
    var file = form.Files["image"];
    if (file != null)
        Console.WriteLine($"Received file: {file.FileName}, size: {file.Length}");
    else
        Console.WriteLine("No file received in form.");
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var content = form["content"].ToString();
    var parentCommentId = form.ContainsKey("parentCommentId") ? int.TryParse(form["parentCommentId"], out var pid) ? pid : (int?)null : null;
    string? imageUrl = null;
    if (file != null && file.Length > 0)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            return Results.BadRequest("Only .jpg, .jpeg, .png files are allowed.");
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "comment-images");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var fileName = $"comment_{userId}_{Guid.NewGuid().ToString().Substring(0,8)}{ext}";
        var filePath = Path.Combine(dir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        imageUrl = $"/comment-images/{fileName}";
    }
    var comment = new SocialComment
    {
        UserId = userId,
        PostId = postId,
        Content = content,
        Timestamp = DateTime.UtcNow,
        ParentCommentId = parentCommentId,
        ImageUrl = imageUrl
    };
    db.SocialComments.Add(comment);
    await db.SaveChangesAsync();
    return Results.Ok(comment);
})
.RequireAuthorization();

app.MapPut("/social/comments/{commentId}", async (ClaimsPrincipal user, int commentId, SocialComment updated, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var isAdmin = user.IsInRole("Admin") || user.IsInRole("Trainer");
    var comment = await db.SocialComments.FindAsync(commentId);
    if (comment == null) return Results.NotFound();
    if (comment.UserId != userId && !isAdmin) return Results.Forbid();
    comment.Content = updated.Content;
    await db.SaveChangesAsync();
    return Results.Ok(comment);
}).RequireAuthorization();

app.MapDelete("/social/comments/{commentId}", async (ClaimsPrincipal user, int commentId, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var isAdmin = user.IsInRole("Admin") || user.IsInRole("Trainer");
    var comment = await db.SocialComments.FindAsync(commentId);
    if (comment == null) return Results.NotFound();
    if (comment.UserId != userId && !isAdmin) return Results.Forbid();
    db.SocialComments.Remove(comment);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/social/{postId}/comments", async (int postId, DataContext db) =>
    Results.Ok(await db.SocialComments
        .Where(c => c.PostId == postId && c.ParentCommentId == null)
        .Include(c => c.User)
        .Include(c => c.Replies)
            .ThenInclude(r => r.User)
        .OrderBy(c => c.Timestamp)
        .Select(c => new {
            id = c.Id,
            content = c.Content,
            timestamp = c.Timestamp,
            imageUrl = c.ImageUrl,
            user = c.User == null ? null : new {
                id = c.User.Id,
                username = c.User.Username,
                avatarUrl = c.User.AvatarUrl
            },
            replies = c.Replies.Select(r => new {
                id = r.Id,
                content = r.Content,
                timestamp = r.Timestamp,
                imageUrl = r.ImageUrl,
                user = r.User == null ? null : new {
                    id = r.User.Id,
                    username = r.User.Username,
                    avatarUrl = r.User.AvatarUrl
                }
            }).ToList()
        })
        .ToListAsync()));

app.MapGet("/admin/users", async (ClaimsPrincipal user, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var users = await db.Users.Select(u => new { u.Id, u.Username, u.Role }).ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization();

app.MapPut("/admin/users/{userId}/role", async (ClaimsPrincipal user, int userId, string role, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var u = await db.Users.FindAsync(userId);
    if (u == null) return Results.NotFound();
    u.Role = role;
    await db.SaveChangesAsync();
    return Results.Ok(u);
}).RequireAuthorization();

app.MapDelete("/admin/users/{userId}", async (ClaimsPrincipal user, int userId, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var u = await db.Users.Include(u => u.DietPlan).Include(u => u.Exercises).Include(u => u.SocialPosts).FirstOrDefaultAsync(u => u.Id == userId);
    if (u == null) return Results.NotFound();
    db.Users.Remove(u);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/admin/diets", async (ClaimsPrincipal user, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var diets = await db.DietPlans.ToListAsync();
    return Results.Ok(diets);
}).RequireAuthorization();

app.MapPut("/admin/diets/{userId}", async (ClaimsPrincipal user, int userId, DietPlan plan, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var existing = await db.DietPlans.FirstOrDefaultAsync(d => d.UserId == userId);
    if (existing == null) return Results.NotFound();
    existing.Calories = plan.Calories;
    existing.Macros = plan.Macros;
    existing.Notes = plan.Notes;
    await db.SaveChangesAsync();
    return Results.Ok(existing);
}).RequireAuthorization();

app.MapGet("/admin/exercises", async (ClaimsPrincipal user, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var exercises = await db.Exercises.ToListAsync();
    return Results.Ok(exercises);
}).RequireAuthorization();

app.MapPut("/admin/exercises/{userId}/{exerciseId}", async (ClaimsPrincipal user, int userId, int exerciseId, Exercise updated, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var ex = await db.Exercises.FirstOrDefaultAsync(e => e.UserId == userId && e.Id == exerciseId);
    if (ex == null) return Results.NotFound();
    ex.Name = updated.Name;
    ex.Description = updated.Description;
    ex.MuscleGroup = updated.MuscleGroup;
    ex.Equipment = updated.Equipment;
    ex.Difficulty = updated.Difficulty;
    ex.Sets = updated.Sets;
    ex.Repetitions = updated.Repetitions;
    ex.RestTimeSeconds = updated.RestTimeSeconds;
    ex.Instructions = updated.Instructions;
    ex.ImageUrl = updated.ImageUrl;
    ex.TimeLimitMinutes = updated.TimeLimitMinutes;
    ex.Notes = updated.Notes;
    ex.Weight = updated.Weight;
    ex.Day = updated.Day;
    await db.SaveChangesAsync();
    return Results.Ok(ex);
}).RequireAuthorization();

app.MapDelete("/admin/exercises/{userId}/{exerciseId}", async (ClaimsPrincipal user, int userId, int exerciseId, DataContext db) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var ex = await db.Exercises.FirstOrDefaultAsync(e => e.UserId == userId && e.Id == exerciseId);
    if (ex == null) return Results.NotFound();
    db.Exercises.Remove(ex);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/messages", async (ClaimsPrincipal user, Message message, DataContext db) =>
{
    var senderId = int.Parse(user.FindFirst("UserId")!.Value);
    message.SenderId = senderId;
    message.Timestamp = DateTime.UtcNow;
    db.Messages.Add(message);
    await db.SaveChangesAsync();
    return Results.Ok(message);
}).RequireAuthorization();

app.MapGet("/messages/{otherUserId}", async (ClaimsPrincipal user, int otherUserId, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var messages = await db.Messages
        .Where(m => (m.SenderId == userId && m.RecipientId == otherUserId) || (m.SenderId == otherUserId && m.RecipientId == userId))
        .OrderBy(m => m.Timestamp)
        .ToListAsync();
    return Results.Ok(messages);
}).RequireAuthorization();

app.MapGet("/messages", async (ClaimsPrincipal user, DataContext db) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var conversations = await db.Messages
        .Where(m => m.SenderId == userId || m.RecipientId == userId)
        .GroupBy(m => m.SenderId == userId ? m.RecipientId : m.SenderId)
        .Select(g => new {
            UserId = g.Key,
            LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault()
        })
        .ToListAsync();
    return Results.Ok(conversations);
}).RequireAuthorization();

app.MapPost("/me/avatar", async (ClaimsPrincipal user, DataContext db, dynamic body) =>
{
    var userId = int.Parse(user.FindFirst("UserId")!.Value);
    var u = await db.Users.FindAsync(userId);
    if (u == null) return Results.NotFound();
    string base64 = (string)body.image;
    var match = Regex.Match(base64, @"data:image/(?<type>.+?);base64,(?<data>.+)");
    if (!match.Success) return Results.BadRequest("Invalid image data");
    var ext = match.Groups["type"].Value == "png" ? ".png" : ".jpg";
    var bytes = Convert.FromBase64String(match.Groups["data"].Value);
    var fileName = $"avatar_{userId}_{Guid.NewGuid().ToString().Substring(0,8)}{ext}";
    var dir = Path.Combine(Directory.GetCurrentDirectory(), "avatars");
    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    var filePath = Path.Combine(dir, fileName);
    await File.WriteAllBytesAsync(filePath, bytes);
    var avatarUrl = $"/avatars/{fileName}";
    u.AvatarUrl = avatarUrl;
    await db.SaveChangesAsync();
    return Results.Ok(new { avatarUrl });
}).RequireAuthorization();

app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "avatars")),
    RequestPath = "/avatars"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "comment-images")),
    RequestPath = "/comment-images"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "post-images")),
    RequestPath = "/post-images"
});

// Public user info endpoints for messaging
app.MapGet("/users", async (DataContext db) =>
{
    var users = await db.Users.Select(u => new { u.Id, u.Username, u.Role, u.AvatarUrl }).ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization();

app.MapGet("/users/{id}", async (int id, DataContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    return Results.Ok(new { user.Id, user.Username, user.Role, user.AvatarUrl });
}).RequireAuthorization();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
