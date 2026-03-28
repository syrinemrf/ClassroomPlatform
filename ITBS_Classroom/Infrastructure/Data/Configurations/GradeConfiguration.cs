using ITBS_Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITBS_Classroom.Infrastructure.Data.Configurations;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.Feedback)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.SubmissionId)
            .IsUnique();

        builder.HasOne(x => x.Submission)
            .WithOne(x => x.Grade)
            .HasForeignKey<Grade>(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Teacher)
            .WithMany(x => x.GradesPublished)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
