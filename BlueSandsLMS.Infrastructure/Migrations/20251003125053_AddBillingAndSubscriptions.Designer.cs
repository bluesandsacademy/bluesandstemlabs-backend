
using System;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{
    [DbContext(typeof(BlueSandsLMSDbContext))]
    [Migration("20251003125053_AddBillingAndSubscriptions")]
    partial class AddBillingAndSubscriptions
    {

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Announcement", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId");

                    b.HasIndex("SchoolId", "ClassroomId", "CreatedAt");

                    b.ToTable("Announcements");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Assignment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("DueAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ResourceCode")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId", "DueAt");

                    b.ToTable("Assignments");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.AuditEvent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("ExtraJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid?>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("Utc")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("Value")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("SchoolId", "Utc", "Category", "Name");

                    b.ToTable("AuditEvents");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.BadgeAward", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("AwardedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("BadgeCode")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "AwardedAt");

                    b.ToTable("BadgeAwards");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Classroom", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("InviteCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("InviteCodeExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<Guid>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("SchoolId", "Name");

                    b.ToTable("Classrooms");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.EmailVerificationToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsUsed")
                        .HasColumnType("bit");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Token")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("EmailVerificationTokens");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Enrollment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("RoleInClass")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("ClassroomId", "UserId");

                    b.ToTable("Enrollments");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ExperimentLaunch", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("Completed")
                        .HasColumnType("bit");

                    b.Property<int>("DurationSec")
                        .HasColumnType("int");

                    b.Property<string>("ExperimentCode")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId", "StartedAt");

                    b.HasIndex("UserId", "StartedAt");

                    b.ToTable("ExperimentLaunches");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ParentLink", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsPrimary")
                        .HasColumnType("bit");

                    b.Property<string>("ParentEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("ParentLinks");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Payment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("AmountKobo")
                        .HasColumnType("bigint");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("PricePerStudent")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("PromoCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Provider")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RawResponse")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Reference")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<int>("StudentsBilled")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Vat")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.ToTable("Payments");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PricingPromo", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("PromoPricePerStudent")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("UsePromoPricing")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("PricingPromos");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PricingTier", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<int>("MaxStudents")
                        .HasColumnType("int");

                    b.Property<int>("MinStudents")
                        .HasColumnType("int");

                    b.Property<decimal>("PricePerStudent")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("TierName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("PricingTiers");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PromoCode", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime?>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<int?>("MaxRedemptions")
                        .HasColumnType("int");

                    b.Property<int>("RedemptionCount")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.ToTable("PromoCodes");

                    b.HasData(
                        new
                        {
                            Id = new Guid("11111111-1111-1111-1111-111111111111"),
                            Code = "ABIAEDTECH25",
                            ExpiresAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            RedemptionCount = 0
                        });
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.QuizAttempt", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("QuizCode")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal>("Score0to1")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId");

                    b.HasIndex("UserId", "CompletedAt");

                    b.ToTable("QuizAttempts");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Role", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Roles");

                    b.HasData(
                        new
                        {
                            Id = new Guid("ae17f104-0ec3-47e3-9517-0e7e2c3be8b0"),
                            Name = "Student"
                        },
                        new
                        {
                            Id = new Guid("d7c51101-d2a4-40d5-bb0a-bd97898cf847"),
                            Name = "Teacher"
                        },
                        new
                        {
                            Id = new Guid("0187a77f-ea11-4f8c-87b5-03d3639adfbb"),
                            Name = "SchoolAdmin"
                        },
                        new
                        {
                            Id = new Guid("83b9ce68-4195-4c10-8e08-3dd6af2b0ec9"),
                            Name = "GlobalAdmin"
                        },
                        new
                        {
                            Id = new Guid("c1aaf5f0-f1d4-4c7e-a1b3-dbb1a8c92d89"),
                            Name = "Parent"
                        });
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.School", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ContactEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ContactName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ContactPhone")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ContactPosition")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Subdomain")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TotalStudents")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Schools");

                    b.HasData(
                        new
                        {
                            Id = new Guid("a1111111-1111-1111-1111-111111111111"),
                            ContactEmail = "",
                            ContactName = "",
                            ContactPhone = "",
                            ContactPosition = "",
                            Country = "",
                            DateCreated = new DateTime(2024, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            Name = "Blue Sands Test School",
                            Subdomain = "bluesands-test",
                            TotalStudents = 0
                        });
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Submission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AssignmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AttachmentContentType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AttachmentName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("AttachmentSizeBytes")
                        .HasColumnType("bigint");

                    b.Property<string>("AttachmentUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Feedback")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("GradedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("GraderUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal?>("Score0to1")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("SubmittedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("StudentId");

                    b.HasIndex("AssignmentId", "StudentId", "Status");

                    b.ToTable("Submissions");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Subscription", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("Active")
                        .HasColumnType("bit");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastPaymentReference")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("PricePerStudent")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("StudentsCovered")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Country")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime?>("EmailVerifiedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsEmailVerified")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLogin")
                        .HasColumnType("datetime2");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Phone")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("RoleId");

                    b.HasIndex("SchoolId");

                    b.ToTable("Users");

                    b.HasData(
                        new
                        {
                            Id = new Guid("36a6c8c6-7f46-4043-939e-382dbb42db2b"),
                            DateCreated = new DateTime(2024, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc),
                            Email = "ifemicheal2@gmail.com",
                            FullName = "Ifedayo Michael",
                            IsActive = true,
                            IsEmailVerified = false,
                            PasswordHash = "$2a$11$cEhjobe.nmtMXJHMXQWhW.a7HJrFSOdBhXdqYi2Oj4BYeTh9LXC2y",
                            RoleId = new Guid("83b9ce68-4195-4c10-8e08-3dd6af2b0ec9")
                        });
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Announcement", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany()
                        .HasForeignKey("ClassroomId");

                    b.HasOne("BlueSandsLMS.Core.Entities.School", "School")
                        .WithMany()
                        .HasForeignKey("SchoolId");

                    b.Navigation("Classroom");

                    b.Navigation("School");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Assignment", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany("Assignments")
                        .HasForeignKey("ClassroomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.BadgeAward", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Classroom", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.School", "School")
                        .WithMany()
                        .HasForeignKey("SchoolId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("School");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.EmailVerificationToken", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Enrollment", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany("Enrollments")
                        .HasForeignKey("ClassroomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ExperimentLaunch", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany()
                        .HasForeignKey("ClassroomId");

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.QuizAttempt", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany()
                        .HasForeignKey("ClassroomId");

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Submission", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Assignment", "Assignment")
                        .WithMany()
                        .HasForeignKey("AssignmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "Student")
                        .WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Assignment");

                    b.Navigation("Student");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.User", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Role", "Role")
                        .WithMany("Users")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.School", "School")
                        .WithMany("Users")
                        .HasForeignKey("SchoolId");

                    b.Navigation("Role");

                    b.Navigation("School");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Classroom", b =>
                {
                    b.Navigation("Assignments");

                    b.Navigation("Enrollments");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Role", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.School", b =>
                {
                    b.Navigation("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
