using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Application.Common;
using DietitianApp.Agent.Application.Models;
using DietitianApp.Agent.Application.Services;
using DietitianApp.Agent.Domain.Entities;
using DietitianApp.Agent.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DietitianApp.Agent.Application.Tests;
public sealed class BatchSendServiceTests
{
    [Fact] public async Task No_group_cannot_create(){Func<Task> act=()=>ActCreate([],"mesaj");await act.Should().ThrowAsync<ApplicationValidationException>();}
    [Fact] public async Task Empty_message_cannot_create(){var f=Fixture();Func<Task> act=()=>f.ActCreate([f.Groups[0].Id]," ");await act.Should().ThrowAsync<ApplicationValidationException>();}
    [Fact] public async Task Unverified_group_is_rejected(){var f=Fixture();f.Groups[0].IsVerified=false;await f.Db.SaveChangesAsync();Func<Task> act=()=>f.ActCreate([f.Groups[0].Id],"x");await act.Should().ThrowAsync<ApplicationValidationException>();}
    [Fact] public async Task Inactive_group_is_rejected(){var f=Fixture();f.Groups[0].IsActive=false;await f.Db.SaveChangesAsync();Func<Task> act=()=>f.ActCreate([f.Groups[0].Id],"x");await act.Should().ThrowAsync<ApplicationValidationException>();}
    [Fact] public async Task Failure_does_not_stop_next_group(){var f=Fixture();f.Gateway.Results.Enqueue(new(false,"X","fail"));f.Gateway.Results.Enqueue(new(true));var b=await f.CreateAll();var done=await f.Service.StartAsync(b.Id,null,default);done.Items.Select(x=>x.Status).Should().ContainInOrder(SendItemStatus.Failed,SendItemStatus.Succeeded);}
    [Fact] public async Task Cancellation_after_send_started_keeps_sent_item_succeeded_and_cancels_pending(){var f=Fixture();var b=await f.CreateAll();using var cts=new CancellationTokenSource();f.Gateway.OnSend=cts.Cancel;var done=await f.Service.StartAsync(b.Id,null,cts.Token);done.Items.Select(x=>x.Status).Should().ContainInOrder(SendItemStatus.Succeeded,SendItemStatus.Cancelled);}
    [Fact] public async Task Successful_item_is_not_in_retry(){var f=Fixture();f.Gateway.Results.Enqueue(new(true));f.Gateway.Results.Enqueue(new(false));var b=await f.CreateAll();var done=await f.Service.StartAsync(b.Id,null,default);var retry=await f.Service.RetryFailuresAsync(done.Id,null,default);retry.TotalCount.Should().Be(1);}
    [Fact] public async Task Cancelled_item_can_be_retried_with_failed_items_but_successful_item_is_skipped(){var f=Fixture();var b=await f.CreateAll();var items=b.Items.ToList();items[0].Status=SendItemStatus.Succeeded;items[1].Status=SendItemStatus.Cancelled;await f.Db.SaveChangesAsync();var retry=await f.Service.RetryUnsuccessfulAsync(b.Id,null,default);retry.TotalCount.Should().Be(1);retry.Items.Should().OnlyContain(x=>x.WhatsAppGroupId==f.Groups[1].Id);}
    [Fact] public async Task Failed_item_can_be_retried(){var f=Fixture();f.Gateway.Results.Enqueue(new(false));f.Gateway.Results.Enqueue(new(true));var b=await f.Service.CreateAsync(new([f.Groups[0].Id],"x"),default);await f.Service.StartAsync(b.Id,null,default);var retry=await f.Service.RetryFailuresAsync(b.Id,null,default);retry.SuccessCount.Should().Be(1);}
    [Fact] public async Task Counters_are_calculated(){var f=Fixture();f.Gateway.Results.Enqueue(new(true));f.Gateway.Results.Enqueue(new(false));var b=await f.CreateAll();var done=await f.Service.StartAsync(b.Id,null,default);done.TotalCount.Should().Be(2);done.SuccessCount.Should().Be(1);done.FailureCount.Should().Be(1);done.Status.Should().Be(SendBatchStatus.CompletedWithErrors);}
    [Fact] public async Task Processing_batch_cannot_start_twice(){var f=Fixture();var b=await f.CreateAll();b.Status=SendBatchStatus.Processing;await f.Db.SaveChangesAsync();await f.Service.Invoking(x=>x.StartAsync(b.Id,null,default)).Should().ThrowAsync<ApplicationValidationException>();}
    private static async Task ActCreate(Guid[] ids,string message){var f=Fixture();await f.Service.CreateAsync(new(ids,message),default);}
    private static TestFixture Fixture()=>new();
    private sealed class TestFixture
    {
        public TestDb Db{get;} public FakeGateway Gateway{get;}=new();public BatchSendService Service{get;}public List<WhatsAppGroup> Groups{get;}=[];
        public TestFixture(){var o=new DbContextOptionsBuilder<TestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;Db=new(o);for(var i=1;i<=2;i++)Groups.Add(new(){Id=Guid.NewGuid(),ExternalName=$"DYT-TEST-00{i}",DisplayName=$"Test {i}",IsActive=true,IsVerified=true});Db.WhatsAppGroups.AddRange(Groups);Db.SaveChanges();Service=new(Db,Gateway,new Clock());}
        public Task<SendBatch> CreateAll()=>Service.CreateAsync(new(Groups.Select(x=>x.Id).ToArray(),"test"),default);
        public Task ActCreate(Guid[] ids,string m)=>Service.CreateAsync(new(ids,m),default);
    }
    private sealed class TestDb(DbContextOptions<TestDb> o):DbContext(o),IApplicationDbContext{public DbSet<WhatsAppGroup> WhatsAppGroups=>Set<WhatsAppGroup>();public DbSet<MessageTemplate> MessageTemplates=>Set<MessageTemplate>();public DbSet<SendBatch> SendBatches=>Set<SendBatch>();public DbSet<SendBatchItem> SendBatchItems=>Set<SendBatchItem>();protected override void OnModelCreating(ModelBuilder b){b.Entity<SendBatch>().HasMany(x=>x.Items).WithOne(x=>x.SendBatch).HasForeignKey(x=>x.SendBatchId);}}
    private sealed class Clock:IClock{public DateTimeOffset UtcNow=>DateTimeOffset.UtcNow;}
    private sealed class FakeGateway:IWhatsAppGateway{public Queue<SendMessageResult> Results{get;}=new();public Action? OnSend{get;set;}public Task<WhatsAppSessionResult> EnsureSessionAsync(CancellationToken t)=>Task.FromResult(new WhatsAppSessionResult(true));public Task<GroupVerificationResult> VerifyGroupAsync(string n,CancellationToken t)=>Task.FromResult(new GroupVerificationResult(true));public Task<SendMessageResult> SendMessageAsync(string n,string m,CancellationToken t){OnSend?.Invoke();t.ThrowIfCancellationRequested();return Task.FromResult(Results.Count>0?Results.Dequeue():new SendMessageResult(true));}}
}
