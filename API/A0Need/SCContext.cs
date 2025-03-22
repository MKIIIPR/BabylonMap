
using AshesMapBib.Models;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging.Console;

//using static BlazorApp1.Models.DTO.SC;

namespace API.A0Need;

public class SCContext : DbContext
{
    public SCContext(DbContextOptions options)
        : base(options)
    {
        Database.SetCommandTimeout(150000);


    }




    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging(); //tie-up DbContext with LoggerFactory object

    }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {


        #region Cyle NoAction
        ////
        //modelBuilder.Entity<Connection>()
        //    .HasOne<NoPowerStats>(e => e.NoPowerStats)
        //    .WithMany()
        //    .HasForeignKey(e => e.Ref)
        //    .IsRequired(true)
        //    .OnDelete(DeleteBehavior.NoAction);

        //modelBuilder.Entity<Connection>()
        //    .HasOne<OverclockStats>(e => e.OverclockStats)
        //    .WithMany()
        //    .HasForeignKey(e => e.Ref)
        //    .IsRequired(true)
        //    .OnDelete(DeleteBehavior.NoAction);
        //modelBuilder.Entity<Connection>()
        //            .HasOne<OverpowerStats>(e => e.OverpowerStats)
        //            .WithMany()
        //            .HasForeignKey(e => e.Ref)
        //            .IsRequired(true)
        //            .OnDelete(DeleteBehavior.NoAction);
        //modelBuilder.Entity<Connection>()
        //            .HasOne<UnderpowerStats>(e => e.UnderpowerStats)
        //            .WithMany()
        //            .HasForeignKey(e => e.Ref)
        //            .IsRequired(true)
        //            .OnDelete(DeleteBehavior.NoAction);
        #endregion



    }
    #region RSI
  
    public DbSet<Node> Nodes { get; set; }
    public DbSet<NodePosition> NodePositions { get; set; }
    #endregion
    #region FPSEquip


    #endregion
    #region GameStats


    #endregion




    //public DbSet<DTO.Ship> Ship { get; set; }
    //public DbSet<DTO.Distortion> Distortions {get;set;}
    //public DbSet<DTO.FuelTank> FuelTanks {get;set;}
    //public DbSet<DTO.Heat> Heats {get;set;}
    //public DbSet<DTO.ItemType> ItemTypes {get;set;}
    //public DbSet<DTO.Loadout> Loadouts {get;set;}
    //public DbSet<DTO.ManufacturerData> ManufacturerDatas {get;set;}
    //public DbSet<DTO.Power> Powers {get;set;}
    //public DbSet<DTO.Shield> Shields {get;set;}
    //public DbSet<DTO.FuelIntake> FuelIntakes { get; set; }
    //public DbSet<DTO.Thruster> Thrusters { get; set; }
    //public DbSet<DTO.Items> Items { get; set; }
    //public DbSet<DTO.Seat> Seats { get; set; }
    //public DbSet<DTO.Radar> Radars { get; set; }
    //public DbSet<DTO.Module> Modules { get; set; }
    //public DbSet<DTO.Dashboard> Dashboards { get; set; }
    //public DbSet<DTO.Cargo> Cargoes { get; set; }
    //public DbSet<DTO.Armor> Armor { get; set; }
    //public DbSet<DTO.Hull> Hulls { get; set; }
    //public DbSet<SimpleShipList> SimpleShipList { get; set; }
    //public DbSet<DTO.Afterburner> Afterburner { get;set; }



}

//Absorption
//Ammo
//AmmoContainer
//Bomb
//ChargeActions
//Connection
//Cooler
//Damage
//Data
//Distortion
//Emp
//Explosion
//FireActions
//FuelTank
//Heat
//HeatParams
//HeatStats
//Interdiction
//Inventory
//ItemType
//Jammer
//Loadout
//ManufacturerData
//Mining
//MiningBooster
//MiningFilter
//MiningLaser
//MiningModifier
//Missile
//MissileRack
//Modifier
//NoPowerStats
//OverclockStats
//OverpowerStats
//Params
//Pierceability
//Port
//Power
//Projectile
//Qdrive
//Regen
//Resistance
//Root
//Shield
//SplineJumpParams
//Spread
//SpreadModifier
//UnderpowerStats
//Weapon
//WeaponModifier
////Dataonly
///public Heat Heat { get; set; }
//Power
//Distortion 
//Weapon 
//AmmoContainer 
//Ammo 
//ManufacturerData 
//List
//Modifier 
//FuelTank 
//Shield  
//Jammer 
//Interdiction 
//Qdrive 
//Missile 
//MissileRack 
//MiningLaser 
//Bomb 
//Cooler 
//Emp 
//Explosion 
//Projectile  
//Damage 
//Pierceability

//Ammo
//AmmoContainer
//ChargeActions
//Connection
//Damage
//Data
//Distortion
//Explosion
//FireActions
//Heat
//HeatStats
//Loadout
//ManufacturerData
//NoPowerStats
//OverclockStats
//OverpowerStats
//Pierceability
//Power
//Projectile
//Regen
//Root
//Spread
//SpreadModifier
//UnderpowerStats
//Weapon
//WeaponData
