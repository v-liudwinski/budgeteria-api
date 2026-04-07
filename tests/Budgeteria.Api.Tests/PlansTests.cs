using System.Net;
using System.Net.Http.Json;
using Budgeteria.Api.Dtos;
using FluentAssertions;

namespace Budgeteria.Api.Tests;

public class PlansTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public PlansTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetPlans_NoPlans_ReturnsEmptyArray()
    {
        var client = _factory.CreateAuthenticatedClient($"auth0|{Guid.NewGuid()}");

        var response = await client.GetAsync("/api/plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<List<FamilyPlanDto>>();
        plans.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlans_WithPlan_ReturnsList()
    {
        var (client, created) = await SetupPlanWithUser();

        var response = await client.GetAsync("/api/plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<List<FamilyPlanDto>>();
        plans.Should().HaveCount(1);
        plans![0].Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task CreatePlan_ReturnsCreatedPlan()
    {
        var client = _factory.CreateAuthenticatedClient($"auth0|{Guid.NewGuid()}");
        await client.GetAsync("/api/auth/me");

        var response = await client.PostAsJsonAsync("/api/plans", MakeCreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var plan = await response.Content.ReadFromJsonAsync<FamilyPlanDto>();
        plan!.Name.Should().Be("Family Budget");
        plan.Currency.Code.Should().Be("USD");
        plan.Categories.Should().HaveCount(1);
        plan.Goals.Should().HaveCount(1);
        plan.Members.Should().HaveCount(1);
        plan.Members[0].Role.Should().Be("admin");
    }

    [Fact]
    public async Task User_CanBelongToMultiplePlans()
    {
        var (clientA, planA) = await SetupPlanWithUser();
        var (clientB, planB) = await SetupPlanWithUser();

        // Invite user A to plan B
        var inviteResp = await clientB.PostAsJsonAsync($"/api/plans/{planB.Id}/members",
            new InviteMemberRequest(null));
        inviteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteLinkResponse>();

        var acceptResp = await clientA.PostAsJsonAsync("/api/plans/members/accept",
            new AcceptInviteRequest(invite!.InviteToken));
        acceptResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // User A should now be in both plans
        var plansResp = await clientA.GetAsync("/api/plans");
        var plans = await plansResp.Content.ReadFromJsonAsync<List<FamilyPlanDto>>();
        plans.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdatePlan_ChangesName()
    {
        var (client, plan) = await SetupPlanWithUser();

        var update = new UpdatePlanRequest("New Name", null, null, null, null);
        var response = await client.PutAsJsonAsync($"/api/plans/{plan.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<FamilyPlanDto>();
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task AnalyzePlan_ReturnsAnalysis()
    {
        var (client, plan) = await SetupPlanWithUser();

        var response = await client.PostAsync($"/api/plans/{plan.Id}/analyze", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var analysis = await response.Content.ReadFromJsonAsync<PlanAnalysisDto>();
        analysis!.OverallScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InviteAndRemoveMember_Works()
    {
        var (client, plan) = await SetupPlanWithUser();

        // Invite (no name — user sets their own)
        var inviteResponse = await client.PostAsJsonAsync($"/api/plans/{plan.Id}/members",
            new InviteMemberRequest("friend@test.com"));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteLinkResponse>();
        invite!.InviteToken.Should().NotBeNullOrEmpty();
        invite.InviteUrl.Should().Contain(invite.InviteToken);

        // Verify pending member was created
        var db = _factory.GetDbContext();
        var pending = db.Members.FirstOrDefault(m => m.Email == "friend@test.com");
        pending.Should().NotBeNull();

        // Remove
        var removeResponse = await client.DeleteAsync($"/api/plans/{plan.Id}/members/{invite.MemberId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AcceptInvitation_LinksUserAndReturnsPlan()
    {
        var (adminClient, adminPlan) = await SetupPlanWithUser();
        var inviteResponse = await adminClient.PostAsJsonAsync($"/api/plans/{adminPlan.Id}/members",
            new InviteMemberRequest(null));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteLinkResponse>();

        var bobSub = $"auth0|{Guid.NewGuid()}";
        var bobClient = _factory.CreateAuthenticatedClient(bobSub, "bob@test.com", "Bob");
        var acceptResponse = await bobClient.PostAsJsonAsync("/api/plans/members/accept",
            new AcceptInviteRequest(invite!.InviteToken));

        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var plan = await acceptResponse.Content.ReadFromJsonAsync<FamilyPlanDto>();
        var bobMember = plan!.Members.FirstOrDefault(m => m.UserId == bobSub);
        bobMember.Should().NotBeNull("Bob should appear as a real member with his Auth0 sub");
        bobMember!.Role.Should().Be("member");

        // Token consumed — replay fails
        var replay = await bobClient.PostAsJsonAsync("/api/plans/members/accept",
            new AcceptInviteRequest(invite.InviteToken));
        replay.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvitation_InvalidToken_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient($"auth0|{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/plans/members/accept",
            new AcceptInviteRequest("nonexistenttoken"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvitation_AlreadyInSamePlan_Returns400()
    {
        var (adminClient, adminPlan) = await SetupPlanWithUser();

        // Generate two invite tokens for the same plan
        var invite1 = await (await adminClient.PostAsJsonAsync($"/api/plans/{adminPlan.Id}/members",
            new InviteMemberRequest(null))).Content.ReadFromJsonAsync<InviteLinkResponse>();
        var invite2 = await (await adminClient.PostAsJsonAsync($"/api/plans/{adminPlan.Id}/members",
            new InviteMemberRequest(null))).Content.ReadFromJsonAsync<InviteLinkResponse>();

        var bobClient = _factory.CreateAuthenticatedClient($"auth0|{Guid.NewGuid()}", "bob2@test.com", "Bob");

        // Bob accepts the first invite
        var first = await bobClient.PostAsJsonAsync("/api/plans/members/accept",
            new AcceptInviteRequest(invite1!.InviteToken));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Bob tries to accept the second invite to the SAME plan → should fail
        var second = await bobClient.PostAsJsonAsync("/api/plans/members/accept",
            new AcceptInviteRequest(invite2!.InviteToken));
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/plans");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(HttpClient Client, FamilyPlanDto Plan)> SetupPlanWithUser()
    {
        var client = _factory.CreateAuthenticatedClient($"auth0|{Guid.NewGuid()}");
        await client.GetAsync("/api/auth/me");
        var response = await client.PostAsJsonAsync("/api/plans", MakeCreateRequest());
        var plan = (await response.Content.ReadFromJsonAsync<FamilyPlanDto>())!;
        return (client, plan);
    }

    private static CreatePlanRequest MakeCreateRequest() => new(
        "Family Budget",
        new CurrencyDto("USD", "$", "en-US"),
        5000,
        [new CreateCategoryRequest("Food", "\U0001f355", "#ff0000", 500, true)],
        [new CreateGoalRequest("Vacation", 3000, 500, "2026-12-31", "medium", 200)]
    );
}
