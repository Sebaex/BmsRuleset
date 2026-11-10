import bms.model.*;
import bms.player.beatoraja.ScoreData;
import bms.player.beatoraja.play.*;
import bms.player.beatoraja.play.JudgeProperty.MissCondition;
import java.util.*;

/** Original score/combo slice composed with original gauge classes; no graphical player. */
public final class FlowOracle {
    static final class Flow {
        final ScoreData score = new ScoreData(Mode.BEAT_7K);
        final GrooveGauge gauge;
        final int[] ghost;
        final long[] recentJudges = new long[100];
        final long[] microrecentJudges = new long[100];
        int recentJudgesIndex;
        final boolean[] combocond = JudgeProperty.LR2.combo;
        final MissCondition miss = JudgeProperty.LR2.miss;
        int combo, coursecombo, coursemaxcombo;
        Flow(int type, int count, double total, float seed) {
            ghost=new int[count]; Arrays.fill(ghost,4);
            Arrays.fill(recentJudges,Long.MIN_VALUE);
            score.setNotes(count); score.setRule(BMSPlayerRule.LR2); score.setJudgeAlgorithm(JudgeAlgorithm.Combo);
            BMSModel model = new BMSModel(); model.setMode(Mode.BEAT_7K);
            model.setJudgerank(3); model.setJudgerankType(BMSModel.JudgeRankType.BMS_RANK);
            model.setTotal(total); model.setTotalType(BMSModel.TotalType.BMS);
            TimeLine[] lines = new TimeLine[count];
            for (int i=0;i<count;i++) { lines[i]=new TimeLine(i,i*1000L,8); lines[i].setNote(1,new NormalNote(0)); }
            model.setAllTimeLine(lines); BMSPlayerRule.validate(model);
            if (model.getTotalNotes()!=count) throw new IllegalStateException("Wrong note count");
            gauge = new GrooveGauge(model,type,GaugeProperty.LR2); gauge.setValue(seed);
        }
        void result(Note n, int judge, long mfast, boolean judgeVanish) {
            // ORIGINAL_SCORE_COMBO_FRAGMENT
            gauge.update(judge); // Original BMSPlayer.update:1181, after updateMicro's score/combo block.
        }
        void mine(double damage) { gauge.addValue((float)-new MineNote(0,damage).getDamage()); }
    }
    public static void main(String[] args) {
        Locale.setDefault(Locale.ROOT);
        for (Class<?> source : new Class<?>[]{GrooveGauge.class,GaugeProperty.class,JudgeProperty.class,
                BMSPlayerRule.class,BMSModel.class,ScoreData.class,Note.class,NormalNote.class,MineNote.class,LaneProperty.class})
            if (!source.getProtectionDomain().getCodeSource().getLocation().equals(FlowOracle.class.getProtectionDomain().getCodeSource().getLocation()))
                throw new IllegalStateException("Not compiled from pinned sources: "+source);
        System.out.println("id,type,notes,total,seed,step,event,health,alive,qualified,combo,maxcombo,ex,passnotes,coursecombo,coursemaxcombo");
        int[] keyLanes=new LaneProperty(Mode.BEAT_7K).getKeyLaneAssign();
        if(keyLanes[0]!=0 || keyLanes[1]!=1) throw new IllegalStateException("Unexpected original 7K key order");
        repeated("normal-clear-200",2,1000,300,20f,0,200);
        repeated("normal-clear-300",2,1000,200,20f,0,300);
        repeated("hard-five-bad",3,1000,300,20f,3,5);
        repeated("exclass-five-bad",7,1000,300,20f,3,5);
        repeated("class-fifteen-bad",6,1000,300,20f,3,15);
        Flow flow = new Flow(2,1000,300,80f);
        row("mixed",2,1000,300,80f,0,"seed",flow);
        int step=0;
        for(int judge:new int[]{0,1,2,3,4,5,0}) { flow.result(new NormalNote(0),judge,0,judge<5); row("mixed",2,1000,300,80f,++step,"j"+judge,flow); }
        flow=new Flow(2,1000,300,80f); row("empty-poor-positive-combo",2,1000,300,80f,0,"seed",flow);
        step=0;
        for(int judge:new int[]{0,5,0}){flow.result(new NormalNote(0),judge,0,judge<5);row("empty-poor-positive-combo",2,1000,300,80f,++step,"j"+judge,flow);}
        flow = new Flow(2,2,12,80f);
        row("mine-between-pg",2,2,12,80f,0,"seed",flow);
        flow.result(new NormalNote(0),0,0,true); row("mine-between-pg",2,2,12,80f,1,"j0",flow);
        flow.mine(20); row("mine-between-pg",2,2,12,80f,2,"mine20",flow);
        flow.result(new NormalNote(0),0,0,true); row("mine-between-pg",2,2,12,80f,3,"j0",flow);
        flow = new Flow(7,1000,300,5.55f);
        row("simultaneous-key-order",7,1000,300,5.55f,0,"seed",flow);
        // Equivalent key state at one update: Java scans keyassign in ascending key order.
        flow.result(new NormalNote(0),0,0,true); row("simultaneous-key-order",7,1000,300,5.55f,1,"j0",flow);
        flow.result(new NormalNote(0),3,150000,true); row("simultaneous-key-order",7,1000,300,5.55f,2,"j3",flow);
        for(float seed:new float[]{2.45f,10f}) {
            String id=seed<3 ? "mine-before-key" : "mine-before-key-control";
            flow=new Flow(7,1000,300,seed); row(id,7,1000,300,seed,0,"seed",flow);
            // Original update: passing mines at258-270 precede pressed-key judgement at364+.
            flow.mine(.5); row(id,7,1000,300,seed,1,"mine0.5",flow);
            flow.result(new NormalNote(0),0,0,true); row(id,7,1000,300,seed,2,"j0",flow);
        }
        flow=new Flow(7,2,300,100f);
        for(int i=1;i<=2;i++){flow.result(new NormalNote(0),0,0,true);row("course-two-stage",7,2,300,100f,i,"j0",flow);}
        int carryCombo=flow.coursecombo,carryMax=flow.coursemaxcombo;
        flow=new Flow(7,3,300,flow.gauge.getValue());
        // JudgeManager.init:216-217 restores these counters from PlayerResource; setters:825-834.
        flow.coursecombo=carryCombo;flow.coursemaxcombo=carryMax;
        for(int i=1;i<=3;i++){flow.result(new NormalNote(0),0,0,true);row("course-two-stage",7,3,300,100f,2+i,"j0",flow);}
    }
    static void repeated(String id,int type,int count,double total,float seed,int judge,int repeats) {
        Flow flow=new Flow(type,count,total,seed); row(id,type,count,total,seed,0,"seed",flow);
        for(int i=1;i<=repeats;i++) { flow.result(new NormalNote(0),judge,0,true); row(id,type,count,total,seed,i,"j"+judge,flow); }
        flow.result(new NormalNote(0),0,0,true); row(id,type,count,total,seed,repeats+1,"j0",flow);
    }
    static void row(String id,int type,int notes,double total,float seed,int step,String event,Flow flow) {
        System.out.printf(Locale.ROOT,"%s,%d,%d,%.17g,%.17g,%d,%s,%.17g,%s,%s,%d,%d,%d,%d,%d,%d%n",
            id,type,notes,total,(double)seed,step,event,(double)flow.gauge.getValue(),flow.gauge.getValue()>0,
            flow.gauge.isQualified(),flow.combo,flow.score.getCombo(),flow.score.getExscore(),flow.score.getPassnotes(),flow.coursecombo,flow.coursemaxcombo);
    }
}
