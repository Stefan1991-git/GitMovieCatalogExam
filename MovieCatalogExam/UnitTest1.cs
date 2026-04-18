using MovieCatalogExam.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace MovieCatalogExam.Tests;

[TestFixture]
public class MovieCatalogTests
{
    private RestClient client;
    private static string lastCreatedMovieId;

    [OneTimeSetUp]
    public void Setup()
    {
        string jwtToken = GetJwtToken("Steven@Steven", "123400");

        var options = new RestClientOptions("http://144.91.123.158:5000")
        {
            Authenticator = new JwtAuthenticator(jwtToken)
        };
        client = new RestClient(options);
    }

    private string GetJwtToken(string email, string password)
    {
        var authClient = new RestClient("http://144.91.123.158:5000");
        var request = new RestRequest("/api/User/Authentication", Method.Post);
        request.AddJsonBody(new { email, password });

        var response = authClient.Execute(request);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return content.GetProperty("accessToken").GetString();
        }
        throw new InvalidOperationException($"Authentication failed: {response.StatusCode}");
    }

    [Order(1)]
    [Test]
    public void CreateMovie_WithRequiredFields_ShouldSucceed()
    {
        var newMovie = new MovieDto
        {
            Title = "Inception",
            Description = "A thief who steals corporate secrets through dream-sharing technology."
        };

        var request = new RestRequest("/api/Movie/Create", Method.Post);
        request.AddJsonBody(newMovie);

        var response = client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        Assert.That(data.Movie, Is.Not.Null);
        Assert.That(data.Movie.Id, Is.Not.Null.Or.Empty);
        Assert.That(data.Msg, Is.EqualTo("Movie created successfully!"));

        lastCreatedMovieId = data.Movie.Id;
    }

    [Order(2)]
    [Test]
    public void EditMovie_ShouldSucceed()
    {
        var updatedMovie = new MovieDto
        {
            Title = "Inception Updated",
            Description = "Updated movie description content."
        };

        var request = new RestRequest("/api/Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", lastCreatedMovieId);
        request.AddJsonBody(updatedMovie);

        var response = client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        Assert.That(data.Msg, Is.EqualTo("Movie edited successfully!"));
    }

    [Order(3)]
    [Test]
    public void GetAllMovies_ShouldReturnNonEmptyArray()
    {
        var request = new RestRequest("/api/Catalog/All", Method.Get);
        var response = client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var movies = JsonSerializer.Deserialize<List<MovieDto>>(response.Content);
        Assert.That(movies, Is.Not.Null.And.Not.Empty);
    }

    [Order(4)]
    [Test]
    public void DeleteMovie_ShouldSucceed()
    {
        var request = new RestRequest("/api/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", lastCreatedMovieId);

        var response = client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        Assert.That(data.Msg, Is.EqualTo("Movie deleted successfully!"));
    }

    [Order(5)]
    [Test]
    public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
    {
        var invalidMovie = new { title = "" };
        var request = new RestRequest("/api/Movie/Create", Method.Post);
        request.AddJsonBody(invalidMovie);

        var response = client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Order(6)]
    [Test]
    public void EditNonExistingMovie_ShouldReturnBadRequest()
    {
        var request = new RestRequest("/api/Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", "non-existing-id");
        request.AddJsonBody(new { title = "Title", description = "Desc" });

        var response = client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        Assert.That(data.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
    }

    [Order(7)]
    [Test]
    public void DeleteNonExistingMovie_ShouldReturnBadRequest()
    {
        var request = new RestRequest("/api/Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", "non-existing-id");

        var response = client.Execute(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
        Assert.That(data.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        client?.Dispose();
    }
}