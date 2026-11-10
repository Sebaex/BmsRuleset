import bms.player.beatoraja.ScoreData;

public final class ScoreOracle {
    public static void main(String[] args) {
        if (!ScoreData.class.getProtectionDomain().getCodeSource().getLocation().equals(ScoreOracle.class.getProtectionDomain().getCodeSource().getLocation()))
            throw new IllegalStateException("ScoreData must come from fixed original source");
        System.out.println("epg,lpg,egr,lgr,exscore");
        for (int[] counts : new int[][]{{0,0,0,0},{1,0,0,0},{0,1,0,0},{0,0,1,1},{7,13,11,17},{1000,999,100,99}}) {
            ScoreData score=new ScoreData(); score.setEpg(counts[0]); score.setLpg(counts[1]); score.setEgr(counts[2]); score.setLgr(counts[3]);
            System.out.printf("%d,%d,%d,%d,%d%n",counts[0],counts[1],counts[2],counts[3],score.getExscore());
        }
    }
}
