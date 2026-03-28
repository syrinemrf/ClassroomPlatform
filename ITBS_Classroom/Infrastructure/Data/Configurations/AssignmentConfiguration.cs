using ITBS_Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITBS_Classroom.Infrastructure.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.AttachmentPath)
            .HasMaxLength(500);

        builder.Property(x => x.AttachmentName)
            .HasMaxLength(255);

        builder.Property(x => x.AttachmentContentType)
            .HasMaxLength(100);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Course)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Teacher)
            .WithMany(x => x.AssignmentsCreated)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
