const SMOOTH = 0.05

// puts our ears where our player's head is
export function setListenerPose(ctx: AudioContext, x: number, y: number, z: number, yaw: number, pitch: number) {
  const l = ctx.listener, t = ctx.currentTime
  const yr = yaw * Math.PI / 180, pr = pitch * Math.PI / 180
  // Minecraft: yaw 0 looks at +Z, positive pitch looks down; both worlds are right-handed with Y up
  const fx = -Math.sin(yr) * Math.cos(pr), fy = -Math.sin(pr), fz = Math.cos(yr) * Math.cos(pr)
  // Firefox (and Zen) has no AudioParams on AudioListener, only the deprecated setters
  if (!l.positionX) {
    l.setPosition(x, y, z)
    l.setOrientation(fx, fy, fz, 0, 1, 0)
    return
  }
  l.positionX.setTargetAtTime(x, t, SMOOTH)
  l.positionY.setTargetAtTime(y, t, SMOOTH)
  l.positionZ.setTargetAtTime(z, t, SMOOTH)
  l.forwardX.setTargetAtTime(fx, t, SMOOTH)
  l.forwardY.setTargetAtTime(fy, t, SMOOTH)
  l.forwardZ.setTargetAtTime(fz, t, SMOOTH)
}
