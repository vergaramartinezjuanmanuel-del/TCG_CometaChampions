namespace CometaChampions.Core.Enums
{
    /// <summary>
    /// Enumeration of all distinct sound effects (SFX) across UI, Collection, Economy, Pack Opening, and Battle.
    /// </summary>
    public enum SfxType
    {
        // UI & General Navigation
        ButtonClick,
        TabSwitch,
        ModalOpen,
        ModalClose,
        ErrorDenied,
        NotificationToast,
        SceneTransition,

        // 3D Card Viewport
        CardFlip,
        CardSnapFront,
        CardGridSelect,
        ReversoSwap,
        FoilShaderChange,

        // Card Collection & Economy
        UpgradeFoilSuccess,
        UpgradeFoilFail,
        CraftCardSuccess,
        CraftCardFail,
        PurchaseCoins,
        BuyAvatar,
        EquipAvatar,
        WatchAdReward,

        // Deck Management
        AddCardToDeck,
        RemoveCardFromDeck,
        AutoCompleteDeck,

        // Pack Opening & Reveal
        PackSelect,
        PackOpen,
        CardRevealFlip,
        NewCardRevealed,
        DuplicateCardRevealed,
        BatchSummary,

        // Daily Rewards
        DailyRewardClaim,
        DailyRewardItemReveal,

        // Battle - Mulligan
        MulliganCardSelect,
        MulliganCardDeselect,
        MulliganConfirmSwap,
        MulliganCardSpinOut,
        MulliganCardTransform,
        MulliganCardSnapReveal,
        MulliganComplete,

        // Battle - Hand & Card
        CardDealSingle,
        CardDealMulti,
        CardHandSelect,
        CardHandDeselect,
        CardDeploySlam,
        CardUndoRecall,

        // Battle - Turn & Energy
        TurnEndCommit,
        TurnStartBell,
        EnergyRefill,
        EnergySpend,
        EnergyRefund,

        // Battle - Arena & Combat
        ArenaCardFacedown,
        ArenaCardReveal,
        LanePowerTick,
        LaneLeaderPlayer,
        LaneLeaderAi,
        LaneLeaderTie,

        // Battle - Game Over
        GameOverVictory,
        GameOverDefeat,
        GameOverTie,
        RewardsGoldCounter,
        RewardsXpSparkle
    }
}
