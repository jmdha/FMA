library(ggplot2)
library(scales)
library(cowplot)

source("./Tools/style.R")

generate_scatterplot <- function(data, name1, name2, title, outName, forceMin = -1) {
	minimum = min(data$x, data$y)
	if (is.na(minimum)) return ()
	if (minimum == 0) minimum <- 1
	if (forceMin != -1)
	  minimum = forceMin
	maximum = max(data$x, data$y)
	if (is.na(maximum)) return ()
	plot <- ggplot(data, aes(x = x, y = y, color=domain, shape=domain)) + 
		scale_shape_manual(values=1:nlevels(factor(data$domain))) +
		geom_point(size=2) +
		geom_abline(intercept = 0, slope = 1, color = "black") +
		  scale_x_log10(
			limits=c(minimum, maximum),
			labels = math_format(
			  format = function(x){number(log10(x), accuracy = 1)}
			)
		) +
		  scale_y_log10(
			limits=c(minimum, maximum),
			labels = math_format(
			  format = function(x){number(log10(x), accuracy = 1)}
			)
		) +
		ggtitle(title) + 
		labs(shape = "", color = "") +
		xlab(name1) +
		ylab(name2) + 
		theme(text = element_text(size=fontSize, family=fontFamily),
			axis.text.x = element_text(angle=90, hjust=1),
			legend.position="bottom"
		) +
		guides(shape=guide_legend(nrow=1, byrow=TRUE))
	ggsave(plot=plot, filename=outName, width=imgWidth, height=imgHeight)
	return (plot)
}

generate_scatterplot_nolegend <- function(data, name1, name2, title, outName, forceMin = -1) {
  minimum = min(data$x, data$y)
  if (is.na(minimum)) return ()
  if (minimum == 0) minimum <- 1
  if (forceMin != -1)
    minimum = forceMin
  maximum = max(data$x, data$y)
  if (is.na(maximum)) return ()
  plot <- ggplot(data, aes(x = x, y = y, color=domain, shape=domain)) + 
    scale_shape_manual(values=1:nlevels(factor(data$domain))) +
    geom_point(size=2) +
    geom_abline(intercept = 0, slope = 1, color = "black") +
    scale_x_log10(
      limits=c(minimum, maximum),
      labels = math_format(
        format = function(x){number(log10(x), accuracy = 1)}
      )
    ) +
    scale_y_log10(
      limits=c(minimum, maximum),
      labels = math_format(
        format = function(x){number(log10(x), accuracy = 1)}
      )
    ) +
    ggtitle(title) + 
    labs(shape = "", color = "") +
    xlab(name1) +
    ylab(name2) + 
    theme(text = element_text(size=fontSize, family=fontFamily),
          axis.text.x = element_text(angle=90, hjust=1),
          legend.position="none"
    ) +
    guides(shape=guide_legend(nrow=1, byrow=TRUE))
  ggsave(plot=plot, filename=outName, width=imgWidth, height=imgHeight_noLegend)
  return (plot)
}

generate_scatterplot_onlylegend <- function(data, name1, name2, title, outName, forceMin = -1) {
  plot <- generate_scatterplot(data, name1, name2, title, outName, forceMin)
  legend <- get_plot_component(plot, 'guide-box-bottom', return_all = TRUE)
  ggsave(plot=legend, filename=outName, width=imgWidth * 3, height=0.5)
  return (legend)
}