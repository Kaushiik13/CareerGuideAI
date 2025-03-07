﻿// <auto-generated />
using System;
using API_V2._0.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareerGuideAI.Migrations
{
    [DbContext(typeof(UPMDbContext))]
    [Migration("20250209150805_localdeployDB")]
    partial class localdeployDB
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Education", b =>
                {
                    b.Property<Guid>("EducationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Degree")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("EndYear")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("FieldOfStudy")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("InstitutionName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("ProfileId")
                        .HasColumnType("uuid");

                    b.Property<string>("StartYear")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("EducationId");

                    b.HasIndex("ProfileId");

                    b.ToTable("Educations");
                });

            modelBuilder.Entity("Skill", b =>
                {
                    b.Property<Guid>("SkillId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProfileId")
                        .HasColumnType("uuid");

                    b.Property<string>("SkillName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("SkillId");

                    b.HasIndex("ProfileId");

                    b.ToTable("Skills");
                });

            modelBuilder.Entity("User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("UserProfile", b =>
                {
                    b.Property<Guid>("ProfileId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateOnly>("DOB")
                        .HasColumnType("date");

                    b.Property<bool>("IsStudent")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Place")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("ProfileId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserProfiles");
                });

            modelBuilder.Entity("WorkExperience", b =>
                {
                    b.Property<Guid>("WorkExperienceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProfileId")
                        .HasColumnType("uuid");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("YearExperience")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("WorkExperienceId");

                    b.HasIndex("ProfileId");

                    b.ToTable("WorkExperiences");
                });

            modelBuilder.Entity("Education", b =>
                {
                    b.HasOne("UserProfile", "UserProfile")
                        .WithMany("Educations")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserProfile");
                });

            modelBuilder.Entity("Skill", b =>
                {
                    b.HasOne("UserProfile", "UserProfile")
                        .WithMany("Skills")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserProfile");
                });

            modelBuilder.Entity("UserProfile", b =>
                {
                    b.HasOne("User", "User")
                        .WithOne("UserProfile")
                        .HasForeignKey("UserProfile", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("WorkExperience", b =>
                {
                    b.HasOne("UserProfile", "UserProfile")
                        .WithMany("WorkExperiences")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserProfile");
                });

            modelBuilder.Entity("User", b =>
                {
                    b.Navigation("UserProfile")
                        .IsRequired();
                });

            modelBuilder.Entity("UserProfile", b =>
                {
                    b.Navigation("Educations");

                    b.Navigation("Skills");

                    b.Navigation("WorkExperiences");
                });
#pragma warning restore 612, 618
        }
    }
}
