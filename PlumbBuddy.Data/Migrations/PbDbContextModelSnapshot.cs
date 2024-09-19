﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlumbBuddy.Data;

#nullable disable

namespace PlumbBuddy.Data.Migrations
{
    [DbContext(typeof(PbDbContext))]
    partial class PbDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("ModCreatorModManifest", b =>
                {
                    b.Property<long>("AttributedModsId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("CreatorsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AttributedModsId", "CreatorsId");

                    b.HasIndex("CreatorsId");

                    b.ToTable("ModCreatorModManifest");
                });

            modelBuilder.Entity("ModCreatorRequiredMod", b =>
                {
                    b.Property<long>("AttributedRequiredModsId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("CreatorsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AttributedRequiredModsId", "CreatorsId");

                    b.HasIndex("CreatorsId");

                    b.ToTable("ModCreatorRequiredMod");
                });

            modelBuilder.Entity("ModFileResourceTopologySnapshot", b =>
                {
                    b.Property<long>("ResourcesId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("TopologySnapshotsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ResourcesId", "TopologySnapshotsId");

                    b.HasIndex("TopologySnapshotsId");

                    b.ToTable("ModFileResourceTopologySnapshot");
                });

            modelBuilder.Entity("ModManifestPackCode", b =>
                {
                    b.Property<long>("RequiredByModsId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("RequiredPacksId")
                        .HasColumnType("INTEGER");

                    b.HasKey("RequiredByModsId", "RequiredPacksId");

                    b.HasIndex("RequiredPacksId");

                    b.ToTable("ModManifestPackCode");
                });

            modelBuilder.Entity("PlumbBuddy.Data.FileOfInterest", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("FileType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Path")
                        .IsUnique();

                    b.ToTable("FilesOfInterest");
                });

            modelBuilder.Entity("PlumbBuddy.Data.IntentionalOverride", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("KeyFullInstance")
                        .HasColumnType("INTEGER");

                    b.Property<int>("KeyGroup")
                        .HasColumnType("INTEGER");

                    b.Property<int>("KeyType")
                        .HasColumnType("INTEGER");

                    b.Property<long>("ModManfiestId")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("ModManifestKeyFullInstance")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ModManifestKeyGroup")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ModManifestKeyType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ModName")
                        .HasColumnType("TEXT");

                    b.Property<string>("ModVersion")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ModManfiestId");

                    b.ToTable("IntentionalOverrides");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModCreator", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.ToTable("ModCreators");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModFile", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("AbsenceNoticed")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("Creation")
                        .HasColumnType("TEXT");

                    b.Property<int>("FileType")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("LastWrite")
                        .HasColumnType("TEXT");

                    b.Property<long>("ModFileHashId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<long?>("Size")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ModFileHashId");

                    b.HasIndex("Path")
                        .IsUnique();

                    b.HasIndex("Path", "Creation", "LastWrite", "Size");

                    b.ToTable("ModFiles");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModFileHash", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long?>("IntentionalOverrideId")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("ModManifestId")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("RequiredModId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ResourcesAndManifestCataloged")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Sha256")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("BLOB")
                        .IsFixedLength();

                    b.HasKey("Id");

                    b.HasIndex("IntentionalOverrideId");

                    b.HasIndex("ModManifestId");

                    b.HasIndex("RequiredModId");

                    b.HasIndex("Sha256")
                        .IsUnique();

                    b.ToTable("ModFileHashes");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModFileResource", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("KeyFullInstance")
                        .HasColumnType("INTEGER");

                    b.Property<int>("KeyGroup")
                        .HasColumnType("INTEGER");

                    b.Property<int>("KeyType")
                        .HasColumnType("INTEGER");

                    b.Property<long>("ModFileHashId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ModFileHashId");

                    b.ToTable("ModFileResources");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModManifest", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .HasColumnType("TEXT");

                    b.Property<string>("Version")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ModManifests");
                });

            modelBuilder.Entity("PlumbBuddy.Data.PackCode", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Code");

                    b.ToTable("PackCodes");
                });

            modelBuilder.Entity("PlumbBuddy.Data.RequiredMod", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long?>("ManifestKeyFullInstance")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ManifestKeyGroup")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ManifestKeyType")
                        .HasColumnType("INTEGER");

                    b.Property<long>("ModManfiestId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .HasColumnType("TEXT");

                    b.Property<string>("Version")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ModManfiestId");

                    b.ToTable("RequiredMods");
                });

            modelBuilder.Entity("PlumbBuddy.Data.TopologySnapshot", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("Taken")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("TopologySnapshots");
                });

            modelBuilder.Entity("ModCreatorModManifest", b =>
                {
                    b.HasOne("PlumbBuddy.Data.ModManifest", null)
                        .WithMany()
                        .HasForeignKey("AttributedModsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PlumbBuddy.Data.ModCreator", null)
                        .WithMany()
                        .HasForeignKey("CreatorsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ModCreatorRequiredMod", b =>
                {
                    b.HasOne("PlumbBuddy.Data.RequiredMod", null)
                        .WithMany()
                        .HasForeignKey("AttributedRequiredModsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PlumbBuddy.Data.ModCreator", null)
                        .WithMany()
                        .HasForeignKey("CreatorsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ModFileResourceTopologySnapshot", b =>
                {
                    b.HasOne("PlumbBuddy.Data.ModFileResource", null)
                        .WithMany()
                        .HasForeignKey("ResourcesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PlumbBuddy.Data.TopologySnapshot", null)
                        .WithMany()
                        .HasForeignKey("TopologySnapshotsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ModManifestPackCode", b =>
                {
                    b.HasOne("PlumbBuddy.Data.ModManifest", null)
                        .WithMany()
                        .HasForeignKey("RequiredByModsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PlumbBuddy.Data.PackCode", null)
                        .WithMany()
                        .HasForeignKey("RequiredPacksId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PlumbBuddy.Data.IntentionalOverride", b =>
                {
                    b.HasOne("PlumbBuddy.Data.ModManifest", "ModManifest")
                        .WithMany("IntentionalOverrides")
                        .HasForeignKey("ModManfiestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModManifest");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModFile", b =>
                {
                    b.HasOne("PlumbBuddy.Data.ModFileHash", "ModFileHash")
                        .WithMany("ModFiles")
                        .HasForeignKey("ModFileHashId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModFileHash");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModFileHash", b =>
                {
                    b.HasOne("PlumbBuddy.Data.IntentionalOverride", null)
                        .WithMany("ModFiles")
                        .HasForeignKey("IntentionalOverrideId");

                    b.HasOne("PlumbBuddy.Data.ModManifest", "ModManifest")
                        .WithMany("SubsumedFiles")
                        .HasForeignKey("ModManifestId");

                    b.HasOne("PlumbBuddy.Data.RequiredMod", null)
                        .WithMany("Files")
                        .HasForeignKey("RequiredModId");

                    b.Navigation("ModManifest");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModFileResource", b =>
                {
                    b.HasOne("PlumbBuddy.Data.ModFileHash", "ModFileHash")
                        .WithMany("Resources")
                        .HasForeignKey("ModFileHashId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModFileHash");
                });

            modelBuilder.Entity("PlumbBuddy.Data.RequiredMod", b =>
                {
                    b.HasOne("PlumbBuddy.Data.ModManifest", "ModManifest")
                        .WithMany("RequiredMods")
                        .HasForeignKey("ModManfiestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ModManifest");
                });

            modelBuilder.Entity("PlumbBuddy.Data.IntentionalOverride", b =>
                {
                    b.Navigation("ModFiles");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModFileHash", b =>
                {
                    b.Navigation("ModFiles");

                    b.Navigation("Resources");
                });

            modelBuilder.Entity("PlumbBuddy.Data.ModManifest", b =>
                {
                    b.Navigation("IntentionalOverrides");

                    b.Navigation("RequiredMods");

                    b.Navigation("SubsumedFiles");
                });

            modelBuilder.Entity("PlumbBuddy.Data.RequiredMod", b =>
                {
                    b.Navigation("Files");
                });
#pragma warning restore 612, 618
        }
    }
}