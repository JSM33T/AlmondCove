﻿using almondCove.Modules;
using almondCove.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Web;

namespace almondCove.Api
{
    public class BlogReply
    {
        public string Slug { get; set; }
        public string CommentId { get; set; }
        [MaxLength(200)]
        [MinLength(2)]
        public string Reply { get; set; }
        public int ReplyId { get; set; }
        public int UserId { get; set; }
        public string ReplyText { get; set; }
    }
    public class BlogComment
    {

        public int Id { get; set; }

        public string Comment { get; set; }

        public string DateCommented { get; set; }

        public int UserId { get; set; }

        public string UsersName { get; set; }

        public string UserName { get; set; }

        public string Slug { get; set; }
    }
    public class BlogLike
    {
        public int UserId { get; set; }
        public int BlogId { get; set; }
        public bool IsLiked { get; set; }
        public string Slug { get; set; }
    }
    public class BlogThumbz
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string UrlHandle { get; set; }
        public string PostContent { get; set; }
        public string Category { get; set; }
        public string Author { get; set; }
        public string Tags { get; set; }
        public string Yr { get; set; }
        public string Locator { get; set; }
        public string PostLikes { get; set; }
        public int Comments { get; set; }
        public int Likes { get; set; }
        public DateTime DatePosted { get; set; }
        public int Id { get; internal set; }
        public string DateFormatted { get; internal set; }
    }
    public class BlogTriggers
    {
        public string Mode { get; set; }
        public string Classifypost { get; set; }
        public string Keypost { get; set; }

    }
    public class BlogCat
    {
        public string Category { get; set; }
        public string BlogNum { get; set; }

    }
    [ApiController]
    public class BlogApiController : ControllerBase
    {
        readonly string connectionString = "";
        private readonly IConfigManager _configManager;
        private readonly ILogger<BlogApiController> _logger;
        public BlogApiController(IConfigManager configManager,ILogger<BlogApiController> logger)
        {
            _configManager = configManager;
            _logger = logger;
            connectionString = _configManager.GetConnString();
            
        }

        [HttpGet("/api/topblogs/get")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetTopBlogs()
        {
            try
            {
                List<BlogThumbz> entries = new();
                string sql;
                string connectionString = _configManager.GetConnString();
                using (SqlConnection connection = new(connectionString))
                {
                    await connection.OpenAsync();
                    sql = $@"
                           SELECT 
                            bm.ID, 
                            bm.Title, 
                            bm.Description, 
                            cat.Title as Category, 
                            cat.Locator,
                            bm.UrlHandle, 
                            bm.DatePosted, 
                            COALESCE(comment_counts.CommentCount, 0) AS CommentCount,
                            COALESCE(like_counts.LikeCount, 0) AS LikeCount
                        FROM TblBlogMaster bm
                        LEFT JOIN (
                            SELECT 
                                PostId, 
                                COUNT(Id) AS CommentCount
                            FROM TblBlogComment
                            GROUP BY PostId
                        ) AS comment_counts ON bm.ID = comment_counts.PostId
                        LEFT JOIN (
                            SELECT 
                                BlogId, 
                                COUNT(Id) AS LikeCount
                            FROM TblBlogLike
                            GROUP BY BlogId
                        ) AS like_counts ON bm.ID = like_counts.BlogId
                        LEFT JOIN TblBlogCategory cat ON bm.CategoryId = cat.Id
                        WHERE bm.IsActive = 1
                        ORDER BY bm.DatePosted DESC
                            ";

                    using SqlCommand command = new(sql, connection);
                    using SqlDataReader dataReader = await command.ExecuteReaderAsync();

                    while (await dataReader.ReadAsync())
                    {
                        BlogThumbz entry = new()
                        {
                            Id = (int)dataReader["Id"],
                            Title = (string)dataReader["Title"],
                            Description = (string)dataReader["Description"],
                            DatePosted = (DateTime)dataReader["DatePosted"],
                            DateFormatted = DateTimeFormats.FormatDateOrRelative((DateTime)dataReader["DatePosted"]),
                            UrlHandle = (string)dataReader["UrlHandle"],
                            Comments = (int)dataReader["CommentCount"],
                            Likes = (int)dataReader["LikeCount"],
                            Category = (string)dataReader["Category"],
                            Locator = (string)dataReader["Locator"],
                        };
                        entries.Add(entry);
                    }
                    await connection.CloseAsync();
                }
                return Ok(entries);
            }
            catch (Exception ex)
            {
                 _logger.LogError("error in get top blogs: " + ex.Message.ToString());
                return BadRequest("something went wrong");
            }
        }

        [HttpGet]
        [Route("api/blogs/{mode}/{classify}/{key}")]
        public async Task<JsonResult> GetBlogs(string mode, string classify, string key)
        {
            mode = "0";
            List<BlogThumbz> thumbs = new();
            _ = new List<BlogThumbz>();
            if (mode != "n")
            {

                using SqlConnection connection = new(connectionString);
                await connection.OpenAsync();
                string sql = "";
                if (classify != "na" && key != "na")
                {
                    if (classify == "category")
                    {
                        sql = "SELECT m.Id, m.Title,m.Description,m.UrlHandle,m.DatePosted,m.Tags,YEAR(m.DatePosted) AS Year,c.Title AS Category,c.Locator,COUNT(bc.Id) AS Comments FROM TblBlogMaster m " +
                              "LEFT JOIN TblBlogComment bc ON m.Id = bc.PostId " +
                              "JOIN TblBlogCategory c ON m.CategoryId = c.Id " +
                              "WHERE c.Locator = '" + key + "' and m.IsActive = 1" +
                              "GROUP BY m.Id, m.Title,m.Description,m.UrlHandle, m.DatePosted,m.Tags,c.Title,c.Locator " +
                              "ORDER BY Id OFFSET " + mode + " " +
                              "ROWS FETCH NEXT 5 ROWS ONLY";
                    }
                    else if (classify == "year")
                    {
                        sql = "SELECT m.Id, m.Title,m.Description,m.UrlHandle,m.DatePosted,m.Tags,YEAR(m.DatePosted) AS Year,c.Title AS Category,c.Locator,COUNT(bc.Id) AS Comments FROM TblBlogMaster m " +
                             "LEFT JOIN TblBlogComment bc ON m.Id = bc.PostId " +
                             "JOIN TblBlogCategory c ON m.CategoryId = c.Id " +
                             "WHERE m.CategoryId = c.Id and YEAR(m.DatePosted) = '" + key + "' AND m.IsActive = 1" +
                             "GROUP BY m.Id, m.Title,m.Description,m.UrlHandle, m.DatePosted,m.Tags,c.Title,c.Locator " +
                             "ORDER BY Id OFFSET " + mode + " " +
                             "ROWS FETCH NEXT 5 ROWS ONLY";

                    }
                    else if (classify == "tag")
                    {
                        sql = "SELECT m.Id, m.Title,m.Description,m.UrlHandle,m.DatePosted,m.Tags,YEAR(m.DatePosted) AS Year,c.Title AS Category,c.Locator,COUNT(bc.Id) AS Comments FROM TblBlogMaster m " +
                             "LEFT JOIN TblBlogComment bc ON m.Id = bc.PostId " +
                             "JOIN TblBlogCategory c ON m.CategoryId = c.Id " +
                             "WHERE m.CategoryId = c.Id and Tags like '%" + key + "%' AND m.IsActive = 1" +
                             "GROUP BY m.Id, m.Title,m.Description,m.UrlHandle, m.DatePosted,m.Tags,c.Title,c.Locator " +
                             "ORDER BY Id OFFSET " + mode + " " +
                             "ROWS FETCH NEXT 5 ROWS ONLY";
                    }

                    else if (classify == "search")
                    {
                        sql = "SELECT m.Id, m.Title,m.Description,m.UrlHandle,m.DatePosted,m.Tags,YEAR(m.DatePosted) AS Year,c.Title AS Category,c.Locator,COUNT(bc.Id) AS Comments FROM TblBlogMaster m " +
                             "LEFT JOIN TblBlogComment bc ON m.Id = bc.PostId " +
                             "JOIN TblBlogCategory c ON m.CategoryId = c.Id " +
                             "WHERE m.CategoryId = c.Id and (m.Title like '%" + key + "%' OR m.Description like '%" + key + "%'  OR m.Tags like '%" + key + "%' ) AND m.IsActive = 1" +
                             "GROUP BY m.Id, m.Title,m.Description,m.UrlHandle, m.DatePosted,m.Tags,c.Title,c.Locator " +
                             "ORDER BY Id OFFSET " + mode + " " +
                             "ROWS FETCH NEXT 5 ROWS ONLY";
                    }
                }
                else
                {
                    sql = "SELECT m.Id, m.Title,m.Description,m.UrlHandle,m.DatePosted,m.Tags,YEAR(m.DatePosted) AS Year,c.Title AS Category,c.Locator,COUNT(bc.Id) AS Comments FROM TblBlogMaster m " +
                            "LEFT JOIN TblBlogComment bc ON m.Id = bc.PostId " +
                            "JOIN TblBlogCategory c ON m.CategoryId = c.Id " +
                            "WHERE m.CategoryId = c.Id  AND m.IsActive = 1" +
                            "GROUP BY m.Id, m.Title,m.Description,m.UrlHandle, m.DatePosted,m.Tags,c.Title,c.Locator " +
                            "ORDER BY Id OFFSET " + mode + " " +
                            "ROWS FETCH NEXT 5 ROWS ONLY";
                }
                using SqlCommand command = new(sql, connection);
                using SqlDataReader dataReader = await command.ExecuteReaderAsync();
                if (dataReader.HasRows)
                {
                    while (await dataReader.ReadAsync())
                    {
                        thumbs.Add(new BlogThumbz
                        {
                            Title = dataReader.GetString(1),
                            Description = dataReader.GetString(2),
                            UrlHandle = dataReader.GetString(3),
                            DatePosted = dataReader.GetDateTime(4),
                            Category = dataReader.GetString(7),
                            Locator = dataReader.GetString(8),
                            Comments = dataReader.GetInt32(9)
                        });
                    }
                }
                await connection.CloseAsync();
            }
            return new JsonResult(thumbs);
        }

        [HttpGet]
        [Route("api/blog/{Slug}/authors")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoadAuthors(string Slug)
        {
            List<object> data = new();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT ba.AuthorId,u.UserName,u.Bio,u.FirstName,u.LastName,avt.Image FROM TblBlogMaster AS b " +
                                        "JOIN TblBlogAuthor AS ba ON ba.BlogId = b.Id " +
                                        "JOIN TblUserProfile AS u ON ba.AuthorId = u.Id " +
                                        "JOIN TblAvatarMaster AS avt ON u.AvatarId = avt.Id where b.UrlHandle = @UrlHandle ", connection);
            //added scalar vars
            command.Parameters.AddWithValue("@UrlHandle", Slug);
            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new
                {
                    userId = reader.GetInt32(0),
                    userName = reader.GetString(1),
                    userBio = reader.GetString(2),
                    firstName = reader.GetString(3),
                    lastName = reader.GetString(4),
                    avatarImage = reader.GetString(5),
                };

                data.Add(row);
            }
            await reader.CloseAsync();
            await connection.CloseAsync();
            return new JsonResult(data);
        }

        [HttpGet]
        [Route("api/blog/{Slug}/likes")]
        [IgnoreAntiforgeryToken]
        public async Task<int> LoadLikes(string Slug)
        {
            List<object> data = new();
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT COUNT(*) FROM TblBlogLike a,TblBlogMaster b where b.UrlHandle = @UrlHandle  and b.Id = a.BlogId", connection);
                command.Parameters.AddWithValue("@UrlHandle", Slug);
                int reader = (int)await command.ExecuteScalarAsync();
                await connection.CloseAsync();
                return reader;
            }
            catch
            {
                return 0;
            }

        }

        [HttpPost]
        [Route("api/blog/addlike")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddLike(BlogLike blogLike)
        {

            if (HttpContext.Session.GetString("user_id") != null)
            {
                List<object> data = new();
                var SessionUserId = HttpContext.Session.GetString("user_id");
                // var SessionUserId = 1;
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT COUNT(*) FROM TblBlogLike a,TblBlogMaster b where a.UserId = @userid and b.UrlHandle = @slug and b.Id = a.BlogId", connection);
                    command.Parameters.AddWithValue("@userid", SessionUserId);
                    command.Parameters.AddWithValue("@slug", blogLike.Slug);
                    int likecounter = (int)await command.ExecuteScalarAsync();

                    SqlCommand blogIdFind = new("SELECT Id from TblBlogMaster where UrlHandle = @blogslug", connection);
                    blogIdFind.Parameters.AddWithValue("@blogslug", blogLike.Slug);
                    int blogId = Convert.ToInt32(blogIdFind.ExecuteScalar());

                    if (likecounter == 1)
                    {
                        command = new SqlCommand("DELETE FROM TblBlogLike where UserId = @userid and BlogId = @blogid", connection);
                        command.Parameters.AddWithValue("@blogid", blogId);
                        command.Parameters.AddWithValue("@userid", SessionUserId);
                        command.ExecuteNonQuery();
                        await connection.CloseAsync();
                        return Ok("Like deleted");
                    }
                    else
                    {
                        SqlCommand maxIdCommand = new("SELECT ISNULL(MAX(Id), 0) + 1 FROM TblBlogLike", connection);
                        int newLikeId = Convert.ToInt32(maxIdCommand.ExecuteScalar());

                        SqlCommand commandIns = new("INSERT INTO TblBlogLike(Id,BlogId,UserId,DateAdded) values(@id,@blogid,@userid,@dateadded)", connection);
                        commandIns.Parameters.AddWithValue("@id", newLikeId);
                        commandIns.Parameters.AddWithValue("@blogid", blogId);
                        commandIns.Parameters.AddWithValue("@userid", SessionUserId);
                        commandIns.Parameters.AddWithValue("@slug", blogLike.Slug);
                        commandIns.Parameters.AddWithValue("@dateadded", DateTime.Now);
                        commandIns.ExecuteNonQuery();
                        //   await connection.CloseAsync();
                        //   await TeleLog.Logstuff("*" + HttpContext.Session.GetString("username") + "* liked a blog:\n\"" + blogLike.Slug + "\"");
                        return Ok("Like added");


                    }

                }
                catch (Exception ex)
                {
                    // Log.Error("load user's isLiked message:" + ex.Message.ToString());
                    return BadRequest("Something went wrong");
                }
            }
            else
            {
                return BadRequest("Unauthorized attempt");
            }
        }

        [HttpPost]
        [Route("api/blog/likestat")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IsLiked(BlogLike blogLike)
        {

            if (HttpContext.Session.GetString("user_id") != null)
            {
                string LoggedInUserId = HttpContext.Session.GetString("user_id").ToString();
                List<object> data = new();
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT COUNT(*) FROM TblBlogLike a,TblBlogMaster b where a.UserId = @userid and b.UrlHandle = @slug and b.Id = a.BlogId", connection);
                    command.Parameters.AddWithValue("@userid", HttpContext.Session.GetString("user_id"));
                    command.Parameters.AddWithValue("@slug", blogLike.Slug);
                    int likecounter = (int)await command.ExecuteScalarAsync();
                    await connection.CloseAsync();
                    if (likecounter == 1)
                    {
                        return Ok(true);
                    }
                    else
                    {
                        return Ok(false);
                    }
                }
                catch (Exception ex)
                {
                    // Log.Error("load user's isLiked message:" + ex.Message.ToString() + " user logged in:" + HttpContext.Session.GetString("username").ToString());
                    return BadRequest();
                }
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("api/blogs/categories/load")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoadCategories()
        {
            List<object> data = new();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var command = new SqlCommand(
                                        @"SELECT
                                            BC.Id AS CategoryId,
                                            BC.Title AS CategoryTitle,
                                            BC.Locator AS CategoryLocator,
                                            COALESCE(COUNT(BM.Id), 0) AS NumberOfActiveItems
                                        FROM 
                                            TblBlogCategory BC

                                        LEFT JOIN (
                                            SELECT CategoryId, Id
                                            FROM TblBlogMaster
                                            WHERE IsActive = 1 -- Add this condition to filter active blogs
                                        ) BM ON BC.Id = BM.CategoryId

                                        GROUP BY 
                                            BC.Id, BC.Title, BC.Locator

                                        ",
                                         connection);

            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new
                {
                    id = reader.GetInt32(0),
                    title = reader.GetString(1),
                    locator = reader.GetString(2),
                    qty = reader.GetInt32(3)
                };

                data.Add(row);
            }
            await reader.CloseAsync();
            await connection.CloseAsync();
            return new JsonResult(data);
        }

        [HttpPost]
        [Route("api/blog/comment/add")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddComment([FromBody] BlogComment blogComment)
        {

            string userid = "", blogid = "";


            if (blogComment.Comment != null && blogComment.Slug != null)
            {

                try
                {
                    string encodedcomment = HttpUtility.HtmlEncode(blogComment.Comment.ToString().Trim());
                    if (encodedcomment.Length >= 3)
                    {
                        using var connection = new SqlConnection(connectionString);
                        await connection.OpenAsync();
                        var command = new SqlCommand("SELECT Id FROM TblBlogMaster WHERE UrlHandle = @urlhandle", connection);
                        command.Parameters.AddWithValue("@urlhandle", blogComment.Slug);
                        var reader = await command.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            blogid = reader.GetInt32(0).ToString();
                        }
                        reader.Close();
                        command = new SqlCommand("SELECT Id FROM TblUserProfile WHERE UserName = @username", connection);
                        command.Parameters.AddWithValue("@username", HttpContext.Session.GetString("username").ToString());
                        reader = await command.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            userid = reader.GetInt32(0).ToString();
                        }
                        reader.Close();
                        SqlCommand maxIdCommand = new("SELECT ISNULL(MAX(Id), 0) + 1 FROM TblBlogComment", connection);
                        int newId = Convert.ToInt32(maxIdCommand.ExecuteScalar());

                        command = new SqlCommand("insert into TblBlogComment(Id,Comment,UserId,PostId,IsActive,DatePosted) values(" + newId + ",'" + encodedcomment + "'," + userid + "," + blogid + ",1,@dateposted)", connection);
                        command.Parameters.Add("@dateposted", SqlDbType.DateTime).Value = DateTime.Now;
                        await command.ExecuteNonQueryAsync();
                        return Ok("Comment added");
                    }
                    else
                    {
                        return BadRequest("Comment too short");
                    }

                }
                catch (Exception ex)
                {
                    // // Log.Information("error in adding comment by" + HttpContext.Session.GetString("username") + "on a blog :" + ex.Message.ToString());
                    return BadRequest("Something went wrong");
                }
            }
            else
            {
                return BadRequest("Something went wrong");
            }

        }

        [HttpPost]
        [Route("api/blog/comments/load")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoadComments([FromBody] BlogComment blogComment)
        {

            Dictionary<int, dynamic> comments = new();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var command = new SqlCommand(@"SELECT
						  c.Id,
						  c.Comment,
						  c.UserId,
						  u.FirstName,
						  u.LastName,
						  u.UserName as CommentUserName,
						  u.Id,
						  LEFT(c.DatePosted, 12),
						  a.Image,
						  r.Id AS ReplyId,
						  r.Reply AS ReplyComment,						  
						LEFT(r.DatePosted, 12),
						  u2.Id AS ReplyUserId,
						  u2.FirstName AS ReplyFirstName,
						  u2.LastName AS ReplyLastName,
						  a2.Image AS ReplyImage,
						u2.UserName AS ReplyUserName
						FROM
						  TblBlogComment c
						  JOIN TblUserProfile u ON c.UserId = u.Id
						  JOIN TblAvatarMaster a ON u.AvatarId = a.Id
						  LEFT JOIN TblBlogReply r ON c.Id = r.CommentId
                          LEFT JOIN TblBlogMaster bm ON bm.Id = c.PostId
						  LEFT JOIN TblUserProfile u2 ON r.UserId = u2.Id
						  LEFT JOIN TblAvatarMaster a2 ON u2.AvatarId = a2.Id
            where bm.UrlHandle = @posturl
						ORDER BY
						  c.DatePosted;
						", connection);
            command.Parameters.AddWithValue("@posturl", blogComment.Slug);
            var reader = await command.ExecuteReaderAsync();
            string user = "";
            bool editable = false, replyeditable = false;
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (HttpContext.Session.GetString("username") != null)
                    {
                        user = "yes";
                        if (reader.GetString(5) == HttpContext.Session.GetString("username").ToString())
                        {
                            editable = true;
                        }
                        else
                        {
                            editable = false;
                        }
                        try
                        {
                            if (reader.GetString(16) == HttpContext.Session.GetString("username").ToString())
                            {
                                replyeditable = true;
                            }
                            else
                            {
                                replyeditable = false;
                            }
                        }
                        catch
                        {
                            replyeditable = false;
                        }
                    }
                    else
                    {
                        user = "no";
                    }
                    var commentId = reader.GetInt32(0);
                    if (!comments.ContainsKey(commentId))
                    {
                        var comment = new
                        {
                            id = commentId,
                            edit = editable,
                            user,
                            fullname = reader.GetString(3) + " " + reader.GetString(4),
                            userid = reader.GetInt32(2),
                            username = reader.GetString(5),
                            comment = HttpUtility.HtmlDecode(reader.GetString(1)),
                            date = reader.GetString(7),
                            avatar = reader.GetString(8),
                            replies = new List<object>()
                        };

                        comments.Add(commentId, comment);
                    }

                    if (!reader.IsDBNull(9))
                    {
                        var reply = new
                        {
                            replyEdit = replyeditable,
                            user,
                            replyId = reader.GetInt32(9),
                            replyComment = HttpUtility.HtmlDecode(reader.GetString(10)),
                            replyUserId = reader.GetInt32(12),
                            replyDate = reader.GetString(11),
                            //replyFirstName = reader.GetString(13),
                            //replyLastName = reader.GetString(14),
                            replyFullName = reader.GetString(13) + " " + reader.GetString(14),
                            replyAvatar = reader.GetString(15)
                        };

                        comments[commentId].replies.Add(reply);
                    }
                }
            }
            else
            {
                //return StatusCode(404);
                string errorMessage = "error";
                return BadRequest(new { error = errorMessage }); // HTTP 400 Bad Request
            }

            await reader.CloseAsync();
            await connection.CloseAsync();
            return new JsonResult(comments.Values);

        }

        [HttpPost]
        [Route("api/blog/comment/edit")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditComment([FromBody] BlogComment blogComment)
        {
            if (HttpContext.Session.GetString("username") != null)
            {
                var Userdet = HttpContext.Session.GetString("user_id").ToString();
                try
                {

                    using var connection = new SqlConnection(connectionString);

                    await connection.OpenAsync();
                    var sql = "UPDATE TblBlogComment SET Comment = @Commentval WHERE Id = @Idval AND UserId = @UserId";
                    var command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@Commentval", HttpUtility.HtmlEncode(blogComment.Comment));
                    command.Parameters.AddWithValue("@Idval", blogComment.Id);
                    command.Parameters.AddWithValue("@UserId", Userdet);
                    await command.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                    return Ok("changes saved");
                }
                catch (Exception ex)
                {
                    // Log.Error("error editing comment by user " + Userdet + "message:" + ex.Message.ToString());
                    return BadRequest("Something went wrong");
                }
            }
            else
            {
                return BadRequest("Access denied");
            }

        }

        [HttpPost]
        [Route("api/blog/comment/delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteComment([FromBody] BlogComment blogComment)
        {
            if (HttpContext.Session.GetString("username") != null)
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Perform multiple database operations within the transaction
                    using var command1 = connection.CreateCommand();
                    command1.Transaction = transaction;
                    command1.CommandText = "DELETE FROM TblBlogComment WHERE Id = @id and UserId = @user_id";
                    command1.Parameters.AddWithValue("@id", blogComment.Id);
                    command1.Parameters.AddWithValue("@user_id", HttpContext.Session.GetString("user_id"));

                    await command1.ExecuteNonQueryAsync();

                    using var command2 = connection.CreateCommand();
                    command2.Transaction = transaction;
                    command2.CommandText = "DELETE FROM TblBlogReply WHERE CommentId = @id";
                    command2.Parameters.AddWithValue("@id", blogComment.Id);
                    command2.Parameters.AddWithValue("@user_id", HttpContext.Session.GetString("user_id"));
                    await command2.ExecuteNonQueryAsync();
                    transaction.Commit();
                    return Ok("Commend deleted");

                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Log.Error("error deleting comment:" + ex.Message.ToString());
                    return BadRequest("Something went wrong");
                }
            }
            else
            {
                return BadRequest("Access Denied");
            }

        }

        [HttpPost]
        [Route("api/blog/reply/add")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddReply([FromBody] BlogReply blogReply)
        {
            string userid = "";
            string encodedreply = HttpUtility.HtmlEncode(blogReply.ReplyText.ToString().Trim());
            if (blogReply.Slug != null)
            {
                try
                {
                    if (encodedreply.Trim() != "")
                    {
                        using var connection = new SqlConnection(connectionString);
                        await connection.OpenAsync();
                        var command = new SqlCommand("SELECT Id FROM TblUserProfile WHERE UserName = @username", connection);
                        command.Parameters.AddWithValue("@username", HttpContext.Session.GetString("username").ToString());
                        var reader = await command.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            userid = reader.GetInt32(0).ToString();
                        }
                        reader.Close();
                        SqlCommand maxIdCommand = new("SELECT ISNULL(MAX(Id), 0) + 1 FROM TblBlogReply", connection);
                        int newId = Convert.ToInt32(maxIdCommand.ExecuteScalar());

                        command = new SqlCommand("insert into TblBlogReply(Id,CommentId,UserId,Reply,IsActive,DatePosted) values(" + newId + ",'" + blogReply.CommentId + "','" + userid + "','" + encodedreply + "',1,@dateposted)", connection);
                        command.Parameters.Add("@dateposted", SqlDbType.DateTime).Value = DateTime.Now;
                        await command.ExecuteNonQueryAsync();
                        return Ok("reply added");
                    }
                    else
                    {

                        return BadRequest("Reply too short");
                    }

                }
                catch (Exception ex)
                {
                    // Log.Information("error in adding reply by" + HttpContext.Session.GetString("username") + "on a blog :" + ex.Message.ToString());
                    return BadRequest("Something went wrong");
                }
            }
            else
            {
                return BadRequest("Invalid request");
            }
        }

        [HttpPost]
        [Route("api/blog/reply/edit")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditrReply([FromBody] BlogReply blogReply)
        {
            if (HttpContext.Session.GetString("username") != null)
            {
                var Userdet = HttpContext.Session.GetString("user_id").ToString();
                try
                {

                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    var sql = "UPDATE TblBlogReply SET Reply = @Replyval WHERE Id = @Idval AND UserId = @UserId ";
                    var command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@Replyval", HttpUtility.HtmlEncode(blogReply.Reply));
                    command.Parameters.AddWithValue("@Idval", blogReply.ReplyId);
                    command.Parameters.AddWithValue("@UserId", HttpContext.Session.GetString("user_id").ToString());
                    await command.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                    return Ok("changes saved");
                }
                catch (Exception ex)
                {
                    // Log.Error("error editing reply by user " + Userdet + "message:" + ex.Message.ToString());
                    return BadRequest("Something went wrong");
                }
            }
            else
            {
                return BadRequest("Access denied");
            }

        }

        [HttpPost]
        [Route("api/blog/reply/delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteReply([FromBody] BlogReply blogReply)
        {
            if (HttpContext.Session.GetString("username") != null)
            {
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    using var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM TblBlogReply WHERE Id = @id and UserId = @user_id";
                    command.Parameters.AddWithValue("@id", blogReply.ReplyId);
                    command.Parameters.AddWithValue("@user_id", HttpContext.Session.GetString("user_id"));
                    await command.ExecuteNonQueryAsync();
                    return Ok("Commend deleted");

                }
                catch
                {
                    return BadRequest("Something went wrong");
                }

            }
            else
            {
                return BadRequest("Access Denied");
            }

        }


    }
}