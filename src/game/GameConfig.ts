import { VIRTUAL_WIDTH } from "@/engine/Renderer";

export const WALL_MARGIN = 20;
export const PLAY_LEFT_BOUND = -(VIRTUAL_WIDTH / 2) + WALL_MARGIN;
export const PLAY_RIGHT_BOUND = VIRTUAL_WIDTH / 2 - WALL_MARGIN;

export const PERFECT_PULSE_DECAY_MS = 420;
export const FEEDBACK_TOAST_MS = 700;

export const MILESTONES: readonly number[] = [10, 25, 50, 75, 100, 150, 200];

export const WIND_AMPLITUDE = 10;
export const WIND_FREQUENCY_HZ = 0.35;

export const SPEED_SHIFT_AMPLITUDE = 0.35; // fraction of base speed
export const SPEED_SHIFT_FREQUENCY_HZ = 0.5;

export const SMALL_START_WIDTH_FACTOR = 0.8;
export const PRECISION_TOLERANCE_FACTOR = 0.6;
export const DOUBLE_SPEED_FACTOR = 1.6;
export const FOG_AMOUNT = 0.55;
export const MOVING_BASE_AMPLITUDE = 7;
export const MOVING_BASE_FREQUENCY_HZ = 0.28;
export const REVERSE_FRACTION_MIN = 0.35;
export const REVERSE_FRACTION_MAX = 0.65;
