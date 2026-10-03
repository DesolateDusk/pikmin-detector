import type { SpotResponse } from '../core/tracker/tracker-data';

export function spotArea(spot: SpotResponse): string {
  return [spot.city, spot.area].filter(Boolean).join('・') || spot.country || '位置未標示';
}

export function distanceKm(spot: SpotResponse): string {
  return spot.distanceMeters === null ? '距離未知' : `約 ${(spot.distanceMeters / 1000).toFixed(1)} 公里`;
}

export function directionsUrl(spot: SpotResponse): string {
  return `https://www.google.com/maps/search/?api=1&query=${spot.latitude},${spot.longitude}`;
}

