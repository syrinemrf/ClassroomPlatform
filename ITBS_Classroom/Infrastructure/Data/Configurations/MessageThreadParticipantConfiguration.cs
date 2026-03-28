using ITBS_Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITBS_Classroom.Infrastructure.Data.Configurations;

public class MessageThreadParticipantConfiguration : IEntityTypeConfiguration<MessageThreadParticipant>
{
    public void Configure(EntityTypeBuilder<MessageThreadParticipant> builder)
    {
        builder.HasKey(x => new { x.ThreadId, x.UserId });

        builder.HasOne(x => x.Thread)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.MessageThreadParticipants)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
