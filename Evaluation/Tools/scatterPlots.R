library(ggplot2)

source("./Tools/style.R")

generate_scatterplot <- function(data, name1, name2, title, outName) {
	minimum = min(data$x, data$y)
	if (is.na(minimum)) return ()
	if (minimum == 0) minimum <- 0.01
	maximum = max(data$x, data$y)
	if (is.na(maximum)) return ()
	plot <- ggplot(data, aes(x = x, y = y, color=domain, shape=domain)) + 
		scale_shape_manual(values=1:nlevels(factor(data$domain))) +
		geom_point(size=2) +
		geom_abline(intercept = 0, slope = 1, color = "black") +
		  scale_x_log10(
			limits=c(minimum, maximum),
			labels = scales::trans_format("log10", scales::math_format(10^.x))
		) +
		  scale_y_log10(
			limits=c(minimum, maximum),
			labels = scales::trans_format("log10", scales::math_format(10^.x))
		) +
		ggtitle(title) + 
		labs(shape = "", color = "") +
		xlab(name1) +
		ylab(name2) + 
		theme(text = element_text(size=fontSize, family=fontFamily),
			axis.text.x = element_text(angle=90, hjust=1),
			legend.position="bottom"
		) +
		guides(shape=guide_legend(nrow=4, byrow=TRUE))
	ggsave(plot=plot, filename=outName, width=imgWidth, height=imgHeight)
	return (plot)
}