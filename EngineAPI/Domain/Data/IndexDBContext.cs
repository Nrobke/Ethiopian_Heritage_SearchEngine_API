using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using EngineAPI.Domain.DataModels;

namespace EngineAPI.Domain.Data
{
    public partial class IndexDBContext : DbContext
    {
        public IndexDBContext()
        {
        }

        public IndexDBContext(DbContextOptions<IndexDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Concept> Concepts { get; set; } = null!;
        public virtual DbSet<Document> Documents { get; set; } = null!;
        public virtual DbSet<DataModels.Index> Indices { get; set; } = null!;
        public virtual DbSet<VwIndicesView> VwIndicesViews { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Concept>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ChildConcept)
                    .HasMaxLength(250)
                    .HasColumnName("childConcept");

                entity.Property(e => e.Concept1)
                    .HasMaxLength(250)
                    .HasColumnName("concept");

                entity.Property(e => e.ParentConcept)
                    .HasMaxLength(250)
                    .HasColumnName("parentConcept");
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Link)
                    .HasMaxLength(250)
                    .HasColumnName("link");

                entity.Property(e => e.Title)
                    .HasMaxLength(500)
                    .HasColumnName("title");
            });

            modelBuilder.Entity<DataModels.Index>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Concept).HasColumnName("concept");

                entity.Property(e => e.ConceptWeight).HasColumnName("conceptWeight");

                entity.Property(e => e.Document).HasColumnName("document");

                entity.Property(e => e.Idf).HasColumnName("idf");

                entity.Property(e => e.Instance)
                    .HasMaxLength(250)
                    .HasColumnName("instance");

                entity.Property(e => e.Keyword)
                    .HasMaxLength(250)
                    .HasColumnName("keyword");

                entity.Property(e => e.Tf).HasColumnName("tf");

                entity.HasOne(d => d.ConceptNavigation)
                    .WithMany(p => p.Indices)
                    .HasForeignKey(d => d.Concept)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Indexs_Concepts");

                entity.HasOne(d => d.DocumentNavigation)
                    .WithMany(p => p.Indices)
                    .HasForeignKey(d => d.Document)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Indexs_Documents");
            });

            modelBuilder.Entity<VwIndicesView>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_IndicesView");

                entity.Property(e => e.Concept).HasColumnName("concept");

                entity.Property(e => e.ConceptDesc)
                    .HasMaxLength(250)
                    .HasColumnName("conceptDesc");

                entity.Property(e => e.ConceptWeight).HasColumnName("conceptWeight");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Document).HasColumnName("document");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Idf).HasColumnName("idf");

                entity.Property(e => e.Instance)
                    .HasMaxLength(250)
                    .HasColumnName("instance");

                entity.Property(e => e.Keyword)
                    .HasMaxLength(250)
                    .HasColumnName("keyword");

                entity.Property(e => e.Link)
                    .HasMaxLength(250)
                    .HasColumnName("link");

                entity.Property(e => e.Tf).HasColumnName("tf");

                entity.Property(e => e.Title)
                    .HasMaxLength(500)
                    .HasColumnName("title");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
