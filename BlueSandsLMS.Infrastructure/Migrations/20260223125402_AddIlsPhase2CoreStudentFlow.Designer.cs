
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
    [Migration("20260223125402_AddIlsPhase2CoreStudentFlow")]
    partial class AddIlsPhase2CoreStudentFlow
    {

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.7")
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
                        .HasColumnType("decimal(18,4)");

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

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "AwardedAt");

                    b.HasIndex("UserId", "Code")
                        .IsUnique();

                    b.ToTable("BadgeAwards");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Certificate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("IssuedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("SubjectCode")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("nvarchar(16)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("SubjectCode");

                    b.HasIndex("UserId", "SubjectCode", "IssuedAt");

                    b.ToTable("Certificates");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ClassMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("nvarchar(4000)");

                    b.Property<Guid>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("FromUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("SentAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId");

                    b.HasIndex("FromUserId");

                    b.ToTable("ClassMessages");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ClassMessageRead", b =>
                {
                    b.Property<Guid>("MessageId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("ReadAt")
                        .HasColumnType("datetime2");

                    b.HasKey("MessageId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("ClassMessageReads");
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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ClassroomTeacher", b =>
                {
                    b.Property<Guid>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("TeacherUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("AssignedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("ClassroomId", "TeacherUserId");

                    b.HasIndex("TeacherUserId");

                    b.ToTable("ClassroomTeachers");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.CurriculumTag", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.HasKey("Id");

                    b.HasIndex("Label", "Subject")
                        .IsUnique();

                    b.ToTable("CurriculumTags");
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

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<int>("DurationSec")
                        .HasColumnType("int");

                    b.Property<DateTime?>("EndedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ExperimentCode")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("ExperimentName")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("LastStep")
                        .HasColumnType("int");

                    b.Property<string>("Mode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("PhETSimulationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("PhETSimulationId");

                    b.HasIndex("ClassroomId", "StartedAt");

                    b.HasIndex("UserId", "StartedAt");

                    b.HasIndex("UserId", "ExperimentName", "StartedAt");

                    b.ToTable("ExperimentLaunches");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ForumPost", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(8000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EditedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("TopicId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("TopicId");

                    b.HasIndex("UserId");

                    b.ToTable("ForumPosts");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ForumTopic", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedById")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("bit");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId");

                    b.HasIndex("CreatedById");

                    b.ToTable("ForumTopics");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.IlsTag", b =>
                {
                    b.Property<Guid>("IlsId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("TagId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("IlsId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("IlsTags");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.IndividualInquiry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DateCreated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<bool>("IsContacted")
                        .HasColumnType("bit");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Notes")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("School")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("DateCreated");

                    b.HasIndex("Email");

                    b.HasIndex("IsContacted");

                    b.ToTable("IndividualInquiries");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.InteractiveLearningSpace", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("DurationMinutes")
                        .HasColumnType("int");

                    b.Property<string>("Grade")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Objective")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<string>("PollOptionsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("SimulationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SimulationId");

                    b.HasIndex("CreatedBy", "Status");

                    b.ToTable("InteractiveLearningSpaces");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Lesson", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("DurationMin")
                        .HasColumnType("int");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.Property<string>("SubjectCode")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("nvarchar(16)");

                    b.Property<string>("Summary")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("SubjectCode", "IsActive", "SortOrder");

                    b.ToTable("Lessons");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.LessonProgress", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("LessonId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId");

                    b.HasIndex("LessonId");

                    b.HasIndex("UserId", "LessonId")
                        .IsUnique();

                    b.ToTable("LessonProgresses");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.MessageLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(5000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Channel")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<Guid?>("ClassroomId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("FromUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("ReadAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("SentAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("ThreadId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ToUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId");

                    b.HasIndex("FromUserId");

                    b.HasIndex("ToUserId");

                    b.ToTable("MessageLogs");
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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PasswordResetToken", b =>
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

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("PasswordResetTokens");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Payment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("AmountKobo")
                        .HasColumnType("bigint");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<decimal>("PricePerStudent")
                        .HasPrecision(18, 2)
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
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Total")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Vat")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.ToTable("Payments");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PhETSimulation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("Biology")
                        .HasColumnType("bit");

                    b.Property<string>("CheerpJRunnable")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("Chemistry")
                        .HasColumnType("bit");

                    b.Property<DateTime>("DateCreated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("Description")
                        .HasMaxLength(3000)
                        .HasColumnType("nvarchar(3000)");

                    b.Property<bool>("EarthSpace")
                        .HasColumnType("bit");

                    b.Property<string>("Filename")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("GradeLevel")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("HighGradeLevel")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Keywords")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime?>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<string>("LearningGoals")
                        .HasMaxLength(3000)
                        .HasColumnType("nvarchar(3000)");

                    b.Property<string>("LowGradeLevel")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("MainTopics")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("MathStatistics")
                        .HasColumnType("bit");

                    b.Property<int?>("NumberOfScreens")
                        .HasColumnType("int");

                    b.Property<string>("PdfUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<bool>("Physics")
                        .HasColumnType("bit");

                    b.Property<string>("Published")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("RunnableResource")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("SampleLearningGoals")
                        .HasMaxLength(3000)
                        .HasColumnType("nvarchar(3000)");

                    b.Property<string>("ScreenNames")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("SimPage")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("SimString")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("SimulationUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Standards")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<string>("TeacherTipsDoc")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("ThumbnailUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Topic")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Translations")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Type")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("Biology");

                    b.HasIndex("Chemistry");

                    b.HasIndex("EarthSpace");

                    b.HasIndex("HighGradeLevel");

                    b.HasIndex("IsActive");

                    b.HasIndex("LowGradeLevel");

                    b.HasIndex("MathStatistics");

                    b.HasIndex("Physics");

                    b.HasIndex("Title");

                    b.ToTable("PhETSimulations");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PricingPromo", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

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
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

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

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("ExperimentLaunchId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("Passed")
                        .HasColumnType("bit");

                    b.Property<string>("QuizCode")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<decimal>("Score0to1")
                        .HasPrecision(5, 4)
                        .HasColumnType("decimal(5,4)");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ClassroomId");

                    b.HasIndex("UserId", "QuizCode", "CompletedAt");

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

                    b.Property<string>("City")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

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

                    b.Property<string>("Lga")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("State")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.SchoolInquiry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ContactPerson")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("DateCreated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("Designation")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("IsContacted")
                        .HasColumnType("bit");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("SchoolName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("StudentCount")
                        .HasColumnType("int");

                    b.Property<int>("TeacherCount")
                        .HasColumnType("int");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("DateCreated");

                    b.HasIndex("Email");

                    b.HasIndex("IsContacted");

                    b.ToTable("SchoolInquiries");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.SessionHypothesis", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("InputMethod")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("SubmittedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasMaxLength(8000)
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SessionId")
                        .IsUnique();

                    b.ToTable("SessionHypotheses");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.SessionPoll", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("OptionIndex")
                        .HasColumnType("int");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("SubmittedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SessionId")
                        .IsUnique();

                    b.ToTable("SessionPolls");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.SessionTrial", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ObservationText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ResultJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("VariablesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SessionId", "CreatedAt");

                    b.ToTable("SessionTrials");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.StudentIlsSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CurrentStep")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(1);

                    b.Property<Guid>("IlsId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("LastSimulationStateJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("IlsId", "CurrentStep");

                    b.HasIndex("StudentId", "IlsId")
                        .IsUnique();

                    b.ToTable("StudentIlsSessions");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Subject", b =>
                {
                    b.Property<string>("Code")
                        .HasMaxLength(16)
                        .HasColumnType("nvarchar(16)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.HasKey("Code");

                    b.HasIndex("IsActive");

                    b.HasIndex("SortOrder");

                    b.ToTable("Subjects");
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
                        .HasPrecision(5, 4)
                        .HasColumnType("decimal(5,4)");

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
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("Active")
                        .HasColumnType("bit");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("LastPaymentReference")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("PricePerStudent")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("StudentsCovered")
                        .HasColumnType("int");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Ticket", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("AssignedToUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(8000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ClosedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<int>("Priority")
                        .HasColumnType("int");

                    b.Property<Guid?>("SchoolId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Source")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("TagsCsv")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("AssignedToUserId");

                    b.HasIndex("CreatedByUserId");

                    b.HasIndex("SchoolId");

                    b.ToTable("Tickets");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.TicketComment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(8000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateModified")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<bool>("IsPrivate")
                        .HasColumnType("bit");

                    b.Property<Guid>("TicketId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("TicketId");

                    b.HasIndex("UserId");

                    b.ToTable("TicketComments");
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

                    b.Property<string>("Gender")
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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Certificate", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Subject", "Subject")
                        .WithMany()
                        .HasForeignKey("SubjectCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Subject");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ClassMessage", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany("Messages")
                        .HasForeignKey("ClassroomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "FromUser")
                        .WithMany()
                        .HasForeignKey("FromUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");

                    b.Navigation("FromUser");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ClassMessageRead", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.ClassMessage", "Message")
                        .WithMany("Reads")
                        .HasForeignKey("MessageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Message");

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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ClassroomTeacher", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany("Teachers")
                        .HasForeignKey("ClassroomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "Teacher")
                        .WithMany()
                        .HasForeignKey("TeacherUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");

                    b.Navigation("Teacher");
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

                    b.HasOne("BlueSandsLMS.Core.Entities.PhETSimulation", "PhETSimulation")
                        .WithMany("Launches")
                        .HasForeignKey("PhETSimulationId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");

                    b.Navigation("PhETSimulation");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ForumPost", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.ForumTopic", "Topic")
                        .WithMany("Posts")
                        .HasForeignKey("TopicId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Topic");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ForumTopic", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany()
                        .HasForeignKey("ClassroomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedById");

                    b.Navigation("Classroom");

                    b.Navigation("CreatedBy");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.IlsTag", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.InteractiveLearningSpace", "Ils")
                        .WithMany("Tags")
                        .HasForeignKey("IlsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.CurriculumTag", "Tag")
                        .WithMany("Ils")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ils");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.InteractiveLearningSpace", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.User", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.PhETSimulation", "Simulation")
                        .WithMany()
                        .HasForeignKey("SimulationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("CreatedByUser");

                    b.Navigation("Simulation");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Lesson", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Subject", "Subject")
                        .WithMany("Lessons")
                        .HasForeignKey("SubjectCode")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Subject");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.LessonProgress", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany()
                        .HasForeignKey("ClassroomId");

                    b.HasOne("BlueSandsLMS.Core.Entities.Lesson", "Lesson")
                        .WithMany("Progresses")
                        .HasForeignKey("LessonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Classroom");

                    b.Navigation("Lesson");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.MessageLog", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Classroom", "Classroom")
                        .WithMany()
                        .HasForeignKey("ClassroomId");

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "FromUser")
                        .WithMany()
                        .HasForeignKey("FromUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "ToUser")
                        .WithMany()
                        .HasForeignKey("ToUserId");

                    b.Navigation("Classroom");

                    b.Navigation("FromUser");

                    b.Navigation("ToUser");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PasswordResetToken", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.SessionHypothesis", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.StudentIlsSession", "Session")
                        .WithOne("Hypothesis")
                        .HasForeignKey("BlueSandsLMS.Core.Entities.SessionHypothesis", "SessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Session");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.SessionPoll", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.StudentIlsSession", "Session")
                        .WithOne("Poll")
                        .HasForeignKey("BlueSandsLMS.Core.Entities.SessionPoll", "SessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Session");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.SessionTrial", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.StudentIlsSession", "Session")
                        .WithMany("Trials")
                        .HasForeignKey("SessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Session");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.StudentIlsSession", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.InteractiveLearningSpace", "Ils")
                        .WithMany("Sessions")
                        .HasForeignKey("IlsId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "Student")
                        .WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Ils");

                    b.Navigation("Student");
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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Ticket", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.User", "AssignedToUser")
                        .WithMany()
                        .HasForeignKey("AssignedToUserId");

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.School", "School")
                        .WithMany()
                        .HasForeignKey("SchoolId");

                    b.Navigation("AssignedToUser");

                    b.Navigation("CreatedByUser");

                    b.Navigation("School");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.TicketComment", b =>
                {
                    b.HasOne("BlueSandsLMS.Core.Entities.Ticket", "Ticket")
                        .WithMany("Comments")
                        .HasForeignKey("TicketId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlueSandsLMS.Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ticket");

                    b.Navigation("User");
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

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ClassMessage", b =>
                {
                    b.Navigation("Reads");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Classroom", b =>
                {
                    b.Navigation("Assignments");

                    b.Navigation("Enrollments");

                    b.Navigation("Messages");

                    b.Navigation("Teachers");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.CurriculumTag", b =>
                {
                    b.Navigation("Ils");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.ForumTopic", b =>
                {
                    b.Navigation("Posts");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.InteractiveLearningSpace", b =>
                {
                    b.Navigation("Sessions");

                    b.Navigation("Tags");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Lesson", b =>
                {
                    b.Navigation("Progresses");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.PhETSimulation", b =>
                {
                    b.Navigation("Launches");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Role", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.School", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.StudentIlsSession", b =>
                {
                    b.Navigation("Hypothesis");

                    b.Navigation("Poll");

                    b.Navigation("Trials");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Subject", b =>
                {
                    b.Navigation("Lessons");
                });

            modelBuilder.Entity("BlueSandsLMS.Core.Entities.Ticket", b =>
                {
                    b.Navigation("Comments");
                });
#pragma warning restore 612, 618
        }
    }
}
