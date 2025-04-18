﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyXProject.Data;

#nullable disable

namespace MyXProject.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20250208023046_migration08")]
    partial class migration08
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");

            modelBuilder.Entity("MyXProject.Common.Entities.Audit", b =>
                {
                    b.Property<Guid>("AuditId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsCreate")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsRead")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsTable")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsUpdate")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("AuditId");

                    b.ToTable("Audit");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.AuditField", b =>
                {
                    b.Property<Guid>("AuditFieldId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("AuditId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsCreate")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsUpdate")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("AuditFieldId");

                    b.HasIndex("AuditId");

                    b.ToTable("AuditField");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.AuditHistory", b =>
                {
                    b.Property<Guid>("AuditHistoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("EntityId")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("Operation")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("TEXT");

                    b.Property<string>("Query")
                        .HasMaxLength(2147483647)
                        .HasColumnType("TEXT");

                    b.Property<string>("Results")
                        .HasMaxLength(2147483647)
                        .HasColumnType("TEXT");

                    b.Property<string>("TableName")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("AuditHistoryId");

                    b.HasIndex("UserId");

                    b.ToTable("AuditHistory");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.AuditHistoryDetail", b =>
                {
                    b.Property<Guid>("AuditHistoryDetailId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("AuditHistoryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("EntityName")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("FieldType")
                        .HasMaxLength(30)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("NewValue")
                        .HasMaxLength(8000)
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("NewValueId")
                        .HasColumnType("TEXT");

                    b.Property<string>("OldValue")
                        .HasMaxLength(8000)
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("OldValueId")
                        .HasColumnType("TEXT");

                    b.HasKey("AuditHistoryDetailId");

                    b.HasIndex("AuditHistoryId");

                    b.ToTable("AuditHistoryDetail");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Job", b =>
                {
                    b.Property<Guid>("JobId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastExecution")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("Queue")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Tag")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("JobId");

                    b.ToTable("Job");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.JobHistory", b =>
                {
                    b.Property<Guid>("JobHistoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CompletedDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("JobId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Ping")
                        .HasColumnType("TEXT");

                    b.Property<string>("ServerName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.HasKey("JobHistoryId");

                    b.HasIndex("JobId");

                    b.HasIndex(new[] { "ServerName", "Ping" }, "IX_JobHistory_ServerName_Ping")
                        .IsDescending(false, true);

                    b.ToTable("JobHistory");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Option", b =>
                {
                    b.Property<Guid>("OptionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Label")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<int?>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Tag")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("OptionId");

                    b.ToTable("Option");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Permission", b =>
                {
                    b.Property<Guid>("PermissionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<string>("Tag")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("PermissionId");

                    b.ToTable("Permission");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Role", b =>
                {
                    b.Property<Guid>("RoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("RoleId");

                    b.ToTable("Role");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.RolePermission", b =>
                {
                    b.Property<Guid>("RolePermissionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("PermissionId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("TEXT");

                    b.HasKey("RolePermissionId");

                    b.HasIndex("PermissionId");

                    b.HasIndex("RoleId");

                    b.ToTable("RolePermission");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Server", b =>
                {
                    b.Property<Guid>("ServerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastPing")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("ServerId");

                    b.ToTable("Server");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Setting", b =>
                {
                    b.Property<Guid>("SettingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasMaxLength(2000)
                        .HasColumnType("TEXT");

                    b.HasKey("SettingId");

                    b.ToTable("Setting");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.System", b =>
                {
                    b.Property<Guid>("SystemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("SystemId");

                    b.ToTable("System");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Team", b =>
                {
                    b.Property<Guid>("TeamId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("TeamId");

                    b.ToTable("Team");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.TeamRole", b =>
                {
                    b.Property<Guid>("TeamRoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("TeamId")
                        .HasColumnType("TEXT");

                    b.HasKey("TeamRoleId");

                    b.HasIndex("RoleId");

                    b.HasIndex("TeamId");

                    b.ToTable("TeamRole");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.TeamUser", b =>
                {
                    b.Property<Guid>("TeamUserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("TeamId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("TeamUserId");

                    b.HasIndex("TeamId");

                    b.HasIndex("UserId");

                    b.ToTable("TeamUser");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(250)
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.ToTable("User");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.UserRole", b =>
                {
                    b.Property<Guid>("UserRoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("UserRoleId");

                    b.HasIndex("RoleId");

                    b.HasIndex("UserId");

                    b.ToTable("UserRole");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.AuditField", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.Audit", "Audit")
                        .WithMany("AuditFields")
                        .HasForeignKey("AuditId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Audit");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.AuditHistory", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.AuditHistoryDetail", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.AuditHistory", "AuditHistory")
                        .WithMany()
                        .HasForeignKey("AuditHistoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AuditHistory");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.JobHistory", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.Job", "Job")
                        .WithMany("JobHistories")
                        .HasForeignKey("JobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Job");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.RolePermission", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.Permission", "Permission")
                        .WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MyXProject.Common.Entities.Role", "Role")
                        .WithMany("RolePermissions")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Permission");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.TeamRole", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.Role", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MyXProject.Common.Entities.Team", "Team")
                        .WithMany("TeamRoles")
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("Team");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.TeamUser", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.Team", "Team")
                        .WithMany()
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MyXProject.Common.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Team");

                    b.Navigation("User");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.UserRole", b =>
                {
                    b.HasOne("MyXProject.Common.Entities.Role", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MyXProject.Common.Entities.User", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Audit", b =>
                {
                    b.Navigation("AuditFields");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Job", b =>
                {
                    b.Navigation("JobHistories");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Role", b =>
                {
                    b.Navigation("RolePermissions");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.Team", b =>
                {
                    b.Navigation("TeamRoles");
                });

            modelBuilder.Entity("MyXProject.Common.Entities.User", b =>
                {
                    b.Navigation("UserRoles");
                });
#pragma warning restore 612, 618
        }
    }
}
